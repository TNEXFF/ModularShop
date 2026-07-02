using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModularShop.Modules.Sales.Contracts;
using ModularShop.Modules.Warehouse.Infrastructure;
using ModularShop.SharedKernel.Messaging;

namespace ModularShop.Modules.Warehouse.Application.IntegrationEventHandlers;

/// <summary>
/// Reacts to the Sales module's <see cref="OrderPlaced"/> event by decrementing stock for each
/// ordered product. Warehouse OWNS the stock data, so the write happens here, in Warehouse's own
/// DbContext and schema — never by another module reaching into these tables.
/// </summary>
internal sealed class DecrementStockOnOrderPlaced : IIntegrationEventHandler<OrderPlaced>
{
    private readonly WarehouseDbContext _db;
    private readonly ILogger<DecrementStockOnOrderPlaced> _logger;

    public DecrementStockOnOrderPlaced(WarehouseDbContext db, ILogger<DecrementStockOnOrderPlaced> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(OrderPlaced @event, CancellationToken cancellationToken = default)
    {
        var productIds = @event.Lines.Select(l => l.ProductId).ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        foreach (var line in @event.Lines)
        {
            var product = products.FirstOrDefault(p => p.Id == line.ProductId);
            product?.DecreaseStock(line.Quantity);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Warehouse decremented stock for order {OrderNumber}.", @event.OrderNumber);
    }
}
