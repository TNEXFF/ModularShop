using Ardalis.Result;
using Ardalis.Specification;
using ModularShop.Modules.Shipping.Domain;

namespace ModularShop.Modules.Shipping.Application;

/// <summary>Use case: advance a shipment from Shipped to Delivered.</summary>
public sealed class DeliverShipment
{
    private readonly IRepositoryBase<Shipment> _shipments;

    public DeliverShipment(IRepositoryBase<Shipment> shipments) => _shipments = shipments;

    public async Task<Result<ShipmentDto>> ExecuteAsync(Guid id, CancellationToken ct)
    {
        var shipment = await _shipments.FirstOrDefaultAsync(new ShipmentByIdForUpdateSpec(id), ct);
        if (shipment is null)
            return Result<ShipmentDto>.NotFound($"Shipment {id} was not found.");

        if (!shipment.Deliver())
            return Result<ShipmentDto>.Invalid(new ValidationError(
                $"Shipment {shipment.ShipmentNumber} cannot be delivered from status '{shipment.Status}'."));

        await _shipments.SaveChangesAsync(ct);
        return Result<ShipmentDto>.Success(shipment.ToDto());
    }
}
