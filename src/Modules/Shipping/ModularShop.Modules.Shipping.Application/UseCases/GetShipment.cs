using Ardalis.Result;
using Ardalis.Specification;
using ModularShop.Modules.Shipping.Domain;

namespace ModularShop.Modules.Shipping.Application;

/// <summary>Use case: fetch a single shipment (with its items) by id.</summary>
public sealed class GetShipment
{
    private readonly IReadRepositoryBase<Shipment> _shipments;

    public GetShipment(IReadRepositoryBase<Shipment> shipments) => _shipments = shipments;

    public async Task<Result<ShipmentDto>> ExecuteAsync(Guid id, CancellationToken ct)
    {
        var shipment = await _shipments.FirstOrDefaultAsync(new ShipmentByIdSpec(id), ct);
        return shipment is null
            ? Result<ShipmentDto>.NotFound($"Shipment {id} was not found.")
            : Result<ShipmentDto>.Success(shipment.ToDto());
    }
}
