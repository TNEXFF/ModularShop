using Microsoft.AspNetCore.Mvc;
using ModularShop.Kernel.Web;
using ModularShop.Modules.Shipping.Application.Dtos;
using ModularShop.Modules.Shipping.Application.UseCases;

namespace ModularShop.Modules.Shipping.Api.Controllers;

/// <summary>
/// Shipment endpoints, including the simple state-advancing actions (Pending → Shipped → Delivered).
/// Each action invokes a single use case and returns the uniform <see cref="ApiResponse{T}"/> envelope.
/// </summary>
[Route("api/shipments")]
public sealed class ShipmentsController : ApiControllerBase
{
    private readonly ListShipments _listShipments;
    private readonly GetShipment _getShipment;
    private readonly ShipShipment _shipShipment;
    private readonly DeliverShipment _deliverShipment;

    public ShipmentsController(
        ListShipments listShipments,
        GetShipment getShipment,
        ShipShipment shipShipment,
        DeliverShipment deliverShipment)
    {
        _listShipments = listShipments;
        _getShipment = getShipment;
        _shipShipment = shipShipment;
        _deliverShipment = deliverShipment;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ShipmentDto>>>> List(CancellationToken ct)
        => ToApiResponse(await _listShipments.ExecuteAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ShipmentDto>>> Get(Guid id, CancellationToken ct)
        => ToApiResponse(await _getShipment.ExecuteAsync(id, ct));

    [HttpPost("{id:guid}/ship")]
    public async Task<ActionResult<ApiResponse<ShipmentDto>>> Ship(Guid id, CancellationToken ct)
        => ToApiResponse(await _shipShipment.ExecuteAsync(id, ct));

    [HttpPost("{id:guid}/deliver")]
    public async Task<ActionResult<ApiResponse<ShipmentDto>>> Deliver(Guid id, CancellationToken ct)
        => ToApiResponse(await _deliverShipment.ExecuteAsync(id, ct));
}
