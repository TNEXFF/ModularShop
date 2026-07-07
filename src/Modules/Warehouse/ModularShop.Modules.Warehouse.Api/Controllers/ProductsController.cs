using Microsoft.AspNetCore.Mvc;
using ModularShop.Kernel.Api;
using ModularShop.Modules.Warehouse.Application.Dtos;
using ModularShop.Modules.Warehouse.Application.UseCases;

namespace ModularShop.Modules.Warehouse.Api.Controllers;

/// <summary>
/// Catalogue endpoints. The controller is thin: it invokes use cases and returns the uniform
/// <see cref="ApiResponse{T}"/> envelope (via <see cref="ApiControllerBase.ToApiResponse{T}"/>).
/// </summary>
[Route("api/products")]
public sealed class ProductsController : ApiControllerBase
{
    private readonly ListProductsUseCase _listProducts;
    private readonly GetProductUseCase _getProduct;

    public ProductsController(ListProductsUseCase listProducts, GetProductUseCase getProduct)
    {
        _listProducts = listProducts;
        _getProduct = getProduct;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductResponse>>>> List(CancellationToken ct)
        => ToApiResponse(await _listProducts.ExecuteAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> Get(Guid id, CancellationToken ct)
        => ToApiResponse(await _getProduct.ExecuteAsync(id, ct));
}
