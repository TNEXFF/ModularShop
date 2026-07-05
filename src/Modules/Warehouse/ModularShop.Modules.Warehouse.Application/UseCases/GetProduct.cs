using Ardalis.Result;
using ModularShop.Kernel.Domain.Repositories;
using ModularShop.Modules.Warehouse.Application.Dtos;
using ModularShop.Modules.Warehouse.Domain;

namespace ModularShop.Modules.Warehouse.Application.UseCases;

/// <summary>Use case: fetch a single product by id.</summary>
public sealed class GetProduct
{
    private readonly IReadRepository<Product> _products;

    public GetProduct(IReadRepository<Product> products) => _products = products;

    public async Task<Result<ProductResponse>> ExecuteAsync(Guid id, CancellationToken ct)
    {
        var product = await _products.FirstOrDefaultAsync(p => p.Id == id, ct);
        return product is null
            ? Result<ProductResponse>.NotFound($"Product {id} was not found.")
            : Result<ProductResponse>.Success(product.ToResponse());
    }
}
