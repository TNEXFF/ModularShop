using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Warehouse.Infrastructure;
using ModularShop.SharedKernel.Domain;

namespace ModularShop.Modules.Warehouse.Application;

/// <summary>Public shape of a product for the catalogue endpoints (this module's own API).</summary>
public sealed record ProductResponse(
    Guid Id, string Sku, string Name, string Description, string Category, decimal Price, int StockQuantity);

/// <summary>
/// Application service for the product catalogue. Queries the Warehouse's own DbContext directly
/// (a perfectly good choice for simple reads) and returns DTOs wrapped in a <see cref="Result"/>.
/// </summary>
internal sealed class ProductService
{
    private readonly WarehouseDbContext _db;

    public ProductService(WarehouseDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<ProductResponse>>> GetProductsAsync(CancellationToken ct)
    {
        var products = await _db.Products.AsNoTracking()
            .OrderBy(p => p.Category).ThenBy(p => p.Name)
            .Select(p => new ProductResponse(p.Id, p.Sku, p.Name, p.Description, p.Category, p.Price, p.StockQuantity))
            .ToListAsync(ct);
        return Result<IReadOnlyList<ProductResponse>>.Success(products);
    }

    public async Task<Result<ProductResponse>> GetProductAsync(Guid id, CancellationToken ct)
    {
        var product = await _db.Products.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductResponse(p.Id, p.Sku, p.Name, p.Description, p.Category, p.Price, p.StockQuantity))
            .FirstOrDefaultAsync(ct);
        return product is null
            ? Result<ProductResponse>.NotFound($"Product {id} was not found.")
            : Result<ProductResponse>.Success(product);
    }
}
