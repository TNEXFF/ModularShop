using Ardalis.Result;
using ModularShop.Kernel.Application.Abstractions;
using ModularShop.Kernel.Domain.Repositories;
using ModularShop.Modules.Shipping.Domain;

namespace ModularShop.Modules.Shipping.Application;

/// <summary>Use case: advance a shipment from Pending to Shipped (assigns carrier + tracking number).</summary>
public sealed class ShipShipment
{
    private readonly IReadRepository<Shipment> _shipments;
    private readonly IUnitOfWork _unitOfWork;

    public ShipShipment(IReadRepository<Shipment> shipments, IUnitOfWork unitOfWork)
    {
        _shipments = shipments;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ShipmentDto>> ExecuteAsync(Guid id, CancellationToken ct)
    {
        // Tracked (with items, for the response DTO) so the state change is persisted on commit.
        var shipment = await _shipments.GetForUpdateAsync(s => s.Id == id, ct, s => s.Items);
        if (shipment is null)
            return Result<ShipmentDto>.NotFound($"Shipment {id} was not found.");

        if (!shipment.Ship("DHL Express", GenerateTrackingNumber()))
            return Result<ShipmentDto>.Invalid(new ValidationError(
                $"Shipment {shipment.ShipmentNumber} cannot be shipped from status '{shipment.Status}'."));

        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShipmentDto>.Success(shipment.ToDto());
    }

    private static string GenerateTrackingNumber() => $"1Z{Random.Shared.Next(100000, 999999)}{Random.Shared.Next(100, 999)}";
}
