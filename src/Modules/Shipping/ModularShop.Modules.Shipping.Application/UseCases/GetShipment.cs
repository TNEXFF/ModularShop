using Ardalis.Result;
using ModularShop.Kernel.Domain.Repositories;
using ModularShop.Modules.Shipping.Application.Dtos;
using ModularShop.Modules.Shipping.Application.Mappings;
using ModularShop.Modules.Shipping.Domain;

namespace ModularShop.Modules.Shipping.Application.UseCases;

/// <summary>Use case: fetch a single shipment (with its items) by id.</summary>
public sealed class GetShipment
{
    private readonly IReadRepository<Shipment> _shipments;

    public GetShipment(IReadRepository<Shipment> shipments) => _shipments = shipments;

    public async Task<Result<ShipmentDto>> ExecuteAsync(Guid id, CancellationToken ct)
    {
        var shipment = await _shipments.GetWithIncludesAsync(s => s.Id == id, ct, s => s.Items);

        return shipment is null
            ? Result<ShipmentDto>.NotFound($"Shipment {id} was not found.")
            : Result<ShipmentDto>.Success(shipment.ToDto());
    }
}
