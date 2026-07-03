using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Warehouse.Domain;

namespace ModularShop.Modules.Warehouse.Application;

/// <summary>Use case: fetch a single product by id.</summary>
public sealed class GetProduct
{
    private readonly DbContext _db;

    public GetProduct(DbContext db) => _db = db;

    public async Task<Result<ProductResponse>> ExecuteAsync(Guid id, CancellationToken ct)
    {
        var product = await _db.Set<Product>().AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return product is null
            ? Result<ProductResponse>.NotFound($"Product {id} was not found.")
            : Result<ProductResponse>.Success(product.ToResponse());
    }
}
