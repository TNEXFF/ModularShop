using Ardalis.Result;
using Ardalis.Specification;
using ModularShop.Modules.Warehouse.Domain;

namespace ModularShop.Modules.Warehouse.Application;

/// <summary>Use case: return the whole product catalogue (ordered by category, then name).</summary>
public sealed class ListProducts
{
    private readonly IReadRepositoryBase<Product> _products;

    public ListProducts(IReadRepositoryBase<Product> products) => _products = products;

    public async Task<Result<IReadOnlyList<ProductResponse>>> ExecuteAsync(CancellationToken ct)
    {
        var products = await _products.ListAsync(new ProductsCatalogSpec(), ct);
        return Result<IReadOnlyList<ProductResponse>>.Success(products.Select(p => p.ToResponse()).ToList());
    }
}
