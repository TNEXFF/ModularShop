using Ardalis.Specification;
using ModularShop.Modules.Warehouse.Contracts;
using ModularShop.Modules.Warehouse.Domain;

namespace ModularShop.Modules.Warehouse.Application;

/// <summary>
/// Implementation of the module's public <see cref="IWarehouseApi"/>. Other modules depend on the
/// interface (in Warehouse.Contracts) and never see this class, the <c>Product</c> entity, or the
/// DbContext. This is the supplier side of a synchronous inter-module call.
/// </summary>
public sealed class WarehouseApi : IWarehouseApi
{
    private readonly IReadRepositoryBase<Product> _products;

    public WarehouseApi(IReadRepositoryBase<Product> products) => _products = products;

    public async Task<IReadOnlyList<ProductStock>> GetProductsAsync(
        IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default)
    {
        var products = await _products.ListAsync(new ProductsByIdsSpec(productIds), cancellationToken);
        return products
            .Select(p => new ProductStock(p.Id, p.Sku, p.Name, p.Price, p.StockQuantity))
            .ToList();
    }
}
