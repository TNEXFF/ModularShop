using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModularShop.Modules.Sales.Contracts;
using ModularShop.Modules.Sales.Domain;
using ModularShop.Modules.Sales.Infrastructure;
using ModularShop.Modules.Warehouse.Contracts;
using ModularShop.SharedKernel.Domain;
using ModularShop.SharedKernel.Identity;
using ModularShop.SharedKernel.Messaging;
using ModularShop.SharedKernel.Persistence;

namespace ModularShop.Modules.Sales.Application;

/// <summary>
/// The Sales application service. <see cref="PlaceOrderAsync"/> is the heart of the demo: it uses
/// BOTH inter-module communication styles — a synchronous call into Warehouse's public API to get
/// prices/stock, then an asynchronous integration event once the order is committed.
/// </summary>
internal sealed class OrderService
{
    private readonly SalesDbContext _db;             // used for reads (ad-hoc queries)
    private readonly IRepository<Order> _orders;     // used for writes (the repository pattern)
    private readonly IWarehouseApi _warehouse;       // Warehouse's PUBLIC interface (sync call)
    private readonly IEventBus _eventBus;            // integration events (async)
    private readonly ICurrentUser _currentUser;      // cross-cutting identity from the shared kernel
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        SalesDbContext db,
        IRepository<Order> orders,
        IWarehouseApi warehouse,
        IEventBus eventBus,
        ICurrentUser currentUser,
        ILogger<OrderService> logger)
    {
        _db = db;
        _orders = orders;
        _warehouse = warehouse;
        _eventBus = eventBus;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<OrderDto>>> GetOrdersAsync(CancellationToken ct)
    {
        var orders = await _db.Orders.AsNoTracking()
            .Include(o => o.Lines)
            .OrderByDescending(o => o.PlacedOnUtc)
            .ToListAsync(ct);
        return Result<IReadOnlyList<OrderDto>>.Success(orders.Select(o => o.ToDto()).ToList());
    }

    public async Task<Result<OrderDto>> GetOrderAsync(Guid id, CancellationToken ct)
    {
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
        return order is null
            ? Result<OrderDto>.NotFound($"Order {id} was not found.")
            : Result<OrderDto>.Success(order.ToDto());
    }

    public async Task<Result<OrderDto>> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct)
    {
        if (request.Lines is null || request.Lines.Count == 0)
            return Result<OrderDto>.Invalid("An order must contain at least one line.");

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, ct);
        if (customer is null)
            return Result<OrderDto>.NotFound($"Customer {request.CustomerId} was not found.");

        // ── SYNCHRONOUS inter-module call ────────────────────────────────────────────────
        // Ask the Warehouse module, THROUGH ITS PUBLIC INTERFACE, for current price and stock.
        // Sales never sees Warehouse's Product entity or DbContext — only the IWarehouseApi contract.
        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = (await _warehouse.GetProductsAsync(productIds, ct)).ToDictionary(p => p.Id);

        var order = new Order(Guid.NewGuid(), GenerateOrderNumber(), customer.Id, customer.Name,
            _currentUser.UserName, DateTime.UtcNow);

        var errors = new List<string>();
        foreach (var line in request.Lines)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
            {
                errors.Add($"Product {line.ProductId} does not exist.");
                continue;
            }
            if (line.Quantity <= 0)
            {
                errors.Add($"Quantity for '{product.Name}' must be greater than zero.");
                continue;
            }
            if (product.StockAvailable < line.Quantity)
            {
                errors.Add($"Insufficient stock for '{product.Name}' (requested {line.Quantity}, available {product.StockAvailable}).");
                continue;
            }

            // Snapshot the price + name into the order line (Sales owns this copy).
            order.AddLine(product.Id, product.Name, product.Price, line.Quantity);
        }

        if (errors.Count > 0)
            return Result<OrderDto>.Invalid(errors.ToArray());

        await _orders.AddAsync(order, ct);
        await _orders.SaveChangesAsync(ct);

        // ── ASYNCHRONOUS integration event ──────────────────────────────────────────────
        // Announce the fact. Warehouse decrements stock and Shipping creates a shipment, each in
        // their own module. Sales does not know or care who reacts.
        var placed = new OrderPlaced(order.Id, order.OrderNumber, customer.Id, customer.Name,
            order.Lines.Select(l => new OrderPlacedLine(l.ProductId, l.ProductName, l.Quantity)).ToList());
        await _eventBus.PublishAsync(placed, ct);

        _logger.LogInformation("Placed order {OrderNumber} for {Customer} ({Lines} lines).",
            order.OrderNumber, customer.Name, order.Lines.Count);

        return Result<OrderDto>.Success(order.ToDto());
    }

    private static string GenerateOrderNumber()
        => $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
}

internal static class SalesMappings
{
    public static OrderDto ToDto(this Order order) => new(
        order.Id,
        order.OrderNumber,
        order.CustomerId,
        order.CustomerName,
        order.Status.ToString(),
        order.PlacedOnUtc,
        order.PlacedBy,
        order.Total,
        order.Lines
            .Select(l => new OrderLineDto(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity, l.LineTotal))
            .ToList());
}
