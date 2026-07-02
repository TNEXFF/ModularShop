using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using ModularShop.Modules.Warehouse.Domain;

namespace ModularShop.Modules.Warehouse.Application;

/// <summary>Input to <see cref="DecrementStock"/>: how many units of a product left stock.</summary>
public sealed record ProductStockChange(Guid ProductId, int Quantity);

/// <summary>
/// Use case: reduce on-hand stock for the given products. Invoked by the Warehouse integration-event
/// handler when an order is placed. Warehouse OWNS the stock data, so the write happens here, against
/// Warehouse's own repository/schema — never by another module reaching into these tables.
/// </summary>
public sealed class DecrementStock
{
    private readonly IRepositoryBase<Product> _products;
    private readonly ILogger<DecrementStock> _logger;

    public DecrementStock(IRepositoryBase<Product> products, ILogger<DecrementStock> logger)
    {
        _products = products;
        _logger = logger;
    }

    public async Task ExecuteAsync(IReadOnlyCollection<ProductStockChange> changes, CancellationToken ct)
    {
        var ids = changes.Select(c => c.ProductId).ToList();
        var products = await _products.ListAsync(new ProductsByIdsForUpdateSpec(ids), ct);

        foreach (var change in changes)
        {
            var product = products.FirstOrDefault(p => p.Id == change.ProductId);
            product?.DecreaseStock(change.Quantity);
        }

        await _products.SaveChangesAsync(ct);
        _logger.LogInformation("Warehouse decremented stock for {Count} product line(s).", changes.Count);
    }
}
