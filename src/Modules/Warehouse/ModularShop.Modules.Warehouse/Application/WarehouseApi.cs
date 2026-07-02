using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Warehouse.Contracts;
using ModularShop.Modules.Warehouse.Infrastructure;

namespace ModularShop.Modules.Warehouse.Application;

/// <summary>
/// Internal implementation of the module's public <see cref="IWarehouseApi"/>. Other modules
/// depend on the interface and never see this class, the <c>Product</c> entity, or
/// <c>WarehouseDbContext</c>. This is the supplier side of a synchronous inter-module call.
/// </summary>
internal sealed class WarehouseApi : IWarehouseApi
{
    private readonly WarehouseDbContext _db;

    public WarehouseApi(WarehouseDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductStock>> GetProductsAsync(
        IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default)
    {
        return await _db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new ProductStock(p.Id, p.Sku, p.Name, p.Price, p.StockQuantity))
            .ToListAsync(cancellationToken);
    }
}
