using Ardalis.Result;
using Ardalis.Specification;
using MediatR;
using Microsoft.Extensions.Logging;
using ModularShop.Kernel.Application;
using ModularShop.Modules.Sales.Contracts;
using ModularShop.Modules.Sales.Domain;
using ModularShop.Modules.Warehouse.Contracts;

namespace ModularShop.Modules.Sales.Application;

/// <summary>
/// The heart of the demo. Placing an order uses BOTH inter-module communication styles:
/// <list type="number">
/// <item>a <b>synchronous</b> call into Warehouse's public API (<see cref="IWarehouseApi"/>) to read
/// current price and stock, then</item>
/// <item>an <b>asynchronous</b> integration event (<see cref="OrderPlaced"/>) published on MediatR
/// once the order is committed — Warehouse decrements stock and Shipping creates a shipment, each in
/// its own module, reacting independently.</item>
/// </list>
/// </summary>
public sealed class PlaceOrder
{
    private readonly IReadRepositoryBase<Customer> _customers;
    private readonly IRepositoryBase<Order> _orders;
    private readonly IWarehouseApi _warehouse;       // Warehouse's PUBLIC interface (sync call)
    private readonly IPublisher _publisher;          // MediatR — publishes integration events (async)
    private readonly ICurrentUser _currentUser;      // cross-cutting identity from the kernel
    private readonly ILogger<PlaceOrder> _logger;

    public PlaceOrder(
        IReadRepositoryBase<Customer> customers,
        IRepositoryBase<Order> orders,
        IWarehouseApi warehouse,
        IPublisher publisher,
        ICurrentUser currentUser,
        ILogger<PlaceOrder> logger)
    {
        _customers = customers;
        _orders = orders;
        _warehouse = warehouse;
        _publisher = publisher;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> ExecuteAsync(PlaceOrderRequest request, CancellationToken ct)
    {
        if (request.Lines is null || request.Lines.Count == 0)
            return Result<OrderDto>.Invalid(new ValidationError("An order must contain at least one line."));

        var customer = await _customers.FirstOrDefaultAsync(new CustomerByIdSpec(request.CustomerId), ct);
        if (customer is null)
            return Result<OrderDto>.NotFound($"Customer {request.CustomerId} was not found.");

        // ── SYNCHRONOUS inter-module call ────────────────────────────────────────────────
        // Ask the Warehouse module, THROUGH ITS PUBLIC INTERFACE, for current price and stock.
        // Sales never sees Warehouse's Product entity or DbContext — only the IWarehouseApi contract.
        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = (await _warehouse.GetProductsAsync(productIds, ct)).ToDictionary(p => p.Id);

        var order = new Order(Guid.NewGuid(), GenerateOrderNumber(), customer.Id, customer.Name,
            _currentUser.UserName, DateTime.UtcNow);

        var errors = new List<ValidationError>();
        foreach (var line in request.Lines)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
            {
                errors.Add(new ValidationError($"Product {line.ProductId} does not exist."));
                continue;
            }
            if (line.Quantity <= 0)
            {
                errors.Add(new ValidationError($"Quantity for '{product.Name}' must be greater than zero."));
                continue;
            }
            if (product.StockAvailable < line.Quantity)
            {
                errors.Add(new ValidationError(
                    $"Insufficient stock for '{product.Name}' (requested {line.Quantity}, available {product.StockAvailable})."));
                continue;
            }

            // Snapshot the price + name into the order line (Sales owns this copy).
            order.AddLine(product.Id, product.Name, product.Price, line.Quantity);
        }

        if (errors.Count > 0)
            return Result<OrderDto>.Invalid(errors);

        await _orders.AddAsync(order, ct);
        await _orders.SaveChangesAsync(ct);

        // ── ASYNCHRONOUS integration event ──────────────────────────────────────────────
        // Announce the fact. Warehouse decrements stock and Shipping creates a shipment, each in its
        // own module. Sales does not know or care who reacts.
        var placed = new OrderPlaced(order.Id, order.OrderNumber, customer.Id, customer.Name,
            order.Lines.Select(l => new OrderPlacedLine(l.ProductId, l.ProductName, l.Quantity)).ToList());
        await _publisher.Publish(placed, ct);

        _logger.LogInformation("Placed order {OrderNumber} for {Customer} ({Lines} lines).",
            order.OrderNumber, customer.Name, order.Lines.Count);

        return Result<OrderDto>.Success(order.ToDto());
    }

    private static string GenerateOrderNumber()
        => $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
}
