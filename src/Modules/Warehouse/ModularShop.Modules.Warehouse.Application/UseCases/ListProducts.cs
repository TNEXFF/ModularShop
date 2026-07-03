using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Warehouse.Domain;

namespace ModularShop.Modules.Warehouse.Application;

/// <summary>Use case: return the whole product catalogue (ordered by category, then name).</summary>
public sealed class ListProducts
{
    private readonly DbContext _db;

    public ListProducts(DbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<ProductResponse>>> ExecuteAsync(CancellationToken ct)
    {
        var products = await _db.Set<Product>()
            .OrderBy(p => p.Category).ThenBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync(ct);

        return Result<IReadOnlyList<ProductResponse>>.Success(products.Select(p => p.ToResponse()).ToList());
    }
}
