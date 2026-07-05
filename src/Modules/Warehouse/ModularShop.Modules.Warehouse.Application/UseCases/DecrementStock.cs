using Microsoft.Extensions.Logging;
using ModularShop.Kernel.Application.Abstractions;
using ModularShop.Kernel.Domain.Repositories;
using ModularShop.Modules.Warehouse.Domain;

namespace ModularShop.Modules.Warehouse.Application.UseCases;

/// <summary>Input to <see cref="DecrementStock"/>: how many units of a product left stock.</summary>
public sealed record ProductStockChange(Guid ProductId, int Quantity);

/// <summary>
/// Use case: reduce on-hand stock for the given products. Invoked by the Warehouse integration-event
/// handler when an order is placed. Warehouse OWNS the stock data, so the write happens here, against
/// Warehouse's own entities — never by another module reaching into these tables. The products are
/// loaded <b>tracked</b> (by key) so the decrement is observed and persisted when the unit of work commits.
/// </summary>
public sealed class DecrementStock
{
    private readonly IReadRepository<Product> _products;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DecrementStock> _logger;

    public DecrementStock(IReadRepository<Product> products, IUnitOfWork unitOfWork, ILogger<DecrementStock> logger)
    {
        _products = products;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteAsync(IReadOnlyCollection<ProductStockChange> changes, CancellationToken ct)
    {
        var ids = changes.Select(c => c.ProductId).ToList();
        var products = await _products.GetByIdsAsync(ids, ct); // tracked

        foreach (var change in changes)
        {
            var product = products.FirstOrDefault(p => p.Id == change.ProductId);
            product?.DecreaseStock(change.Quantity);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Warehouse decremented stock for {Count} product line(s).", changes.Count);
    }
}
