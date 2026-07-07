using Microsoft.AspNetCore.Mvc;
using ModularShop.Kernel.Api;
using ModularShop.Modules.Sales.Application.Dtos;
using ModularShop.Modules.Sales.Application.UseCases;

namespace ModularShop.Modules.Sales.Api.Controllers;

/// <summary>
/// Order endpoints. Each action invokes a single use case and returns the uniform
/// <see cref="ApiResponse{T}"/> envelope — the controller holds no business logic.
/// </summary>
[Route("api/orders")]
public sealed class OrdersController : ApiControllerBase
{
    private readonly GetOrdersUseCase _getOrders;
    private readonly GetOrderUseCase _getOrder;
    private readonly PlaceOrderUseCase _placeOrder;

    public OrdersController(GetOrdersUseCase getOrders, GetOrderUseCase getOrder, PlaceOrderUseCase placeOrder)
    {
        _getOrders = getOrders;
        _getOrder = getOrder;
        _placeOrder = placeOrder;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrderDto>>>> List(CancellationToken ct)
        => ToApiResponse(await _getOrders.ExecuteAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> Get(Guid id, CancellationToken ct)
        => ToApiResponse(await _getOrder.ExecuteAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderDto>>> Place([FromBody] PlaceOrderRequest request, CancellationToken ct)
        => ToApiResponse(await _placeOrder.ExecuteAsync(request, ct));
}
