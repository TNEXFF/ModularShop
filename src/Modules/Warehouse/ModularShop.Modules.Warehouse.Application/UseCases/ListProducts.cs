using Ardalis.Result;
using ModularShop.Kernel.Domain.Repositories;
using ModularShop.Modules.Warehouse.Domain;

namespace ModularShop.Modules.Warehouse.Application;

/// <summary>Use case: return the whole product catalogue (ordered by category, then name).</summary>
public sealed class ListProducts
{
    private readonly IReadRepository<Product> _products;

    public ListProducts(IReadRepository<Product> products) => _products = products;

    public async Task<Result<IReadOnlyList<ProductResponse>>> ExecuteAsync(CancellationToken ct)
    {
        var products = await _products.ListAsync(
            orderBy: q => q.OrderBy(p => p.Category).ThenBy(p => p.Name),
            cancellationToken: ct);

        return Result<IReadOnlyList<ProductResponse>>.Success(products.Select(p => p.ToResponse()).ToList());
    }
}
