using Ardalis.Result;
using Ardalis.Specification;
using ModularShop.Modules.Shipping.Domain;

namespace ModularShop.Modules.Shipping.Application;

/// <summary>Use case: advance a shipment from Pending to Shipped (assigns carrier + tracking number).</summary>
public sealed class ShipShipment
{
    private readonly IRepositoryBase<Shipment> _shipments;

    public ShipShipment(IRepositoryBase<Shipment> shipments) => _shipments = shipments;

    public async Task<Result<ShipmentDto>> ExecuteAsync(Guid id, CancellationToken ct)
    {
        var shipment = await _shipments.FirstOrDefaultAsync(new ShipmentByIdForUpdateSpec(id), ct);
        if (shipment is null)
            return Result<ShipmentDto>.NotFound($"Shipment {id} was not found.");

        if (!shipment.Ship("DHL Express", GenerateTrackingNumber()))
            return Result<ShipmentDto>.Invalid(new ValidationError(
                $"Shipment {shipment.ShipmentNumber} cannot be shipped from status '{shipment.Status}'."));

        await _shipments.SaveChangesAsync(ct);
        return Result<ShipmentDto>.Success(shipment.ToDto());
    }

    private static string GenerateTrackingNumber() => $"1Z{Random.Shared.Next(100000, 999999)}{Random.Shared.Next(100, 999)}";
}
