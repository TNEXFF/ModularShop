using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModularShop.Modules.Warehouse.Domain;

namespace ModularShop.Modules.Warehouse.Application;

/// <summary>Input to <see cref="DecrementStock"/>: how many units of a product left stock.</summary>
public sealed record ProductStockChange(Guid ProductId, int Quantity);

/// <summary>
/// Use case: reduce on-hand stock for the given products. Invoked by the Warehouse integration-event
/// handler when an order is placed. Warehouse OWNS the stock data, so the write happens here, against
/// Warehouse's own entities — never by another module reaching into these tables. The products are
/// loaded <b>tracked</b> so the decrement persists on SaveChanges.
/// </summary>
public sealed class DecrementStock
{
    private readonly DbContext _db;
    private readonly ILogger<DecrementStock> _logger;

    public DecrementStock(DbContext db, ILogger<DecrementStock> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(IReadOnlyCollection<ProductStockChange> changes, CancellationToken ct)
    {
        var ids = changes.Select(c => c.ProductId).ToList();
        var products = await _db.Set<Product>().Where(p => ids.Contains(p.Id)).ToListAsync(ct);

        foreach (var change in changes)
        {
            var product = products.FirstOrDefault(p => p.Id == change.ProductId);
            product?.DecreaseStock(change.Quantity);
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Warehouse decremented stock for {Count} product line(s).", changes.Count);
    }
}
