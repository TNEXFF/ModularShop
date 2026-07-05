using Ardalis.Result;
using ModularShop.Kernel.Application.Abstractions;
using ModularShop.Kernel.Domain.Repositories;
using ModularShop.Modules.Shipping.Application.Dtos;
using ModularShop.Modules.Shipping.Application.Mappings;
using ModularShop.Modules.Shipping.Domain;

namespace ModularShop.Modules.Shipping.Application.UseCases;

/// <summary>Use case: advance a shipment from Shipped to Delivered.</summary>
public sealed class DeliverShipment
{
    private readonly IReadRepository<Shipment> _shipments;
    private readonly IUnitOfWork _unitOfWork;

    public DeliverShipment(IReadRepository<Shipment> shipments, IUnitOfWork unitOfWork)
    {
        _shipments = shipments;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ShipmentDto>> ExecuteAsync(Guid id, CancellationToken ct)
    {
        var shipment = await _shipments.GetForUpdateAsync(s => s.Id == id, ct, s => s.Items); // tracked
        if (shipment is null)
            return Result<ShipmentDto>.NotFound($"Shipment {id} was not found.");

        if (!shipment.Deliver())
            return Result<ShipmentDto>.Invalid(new ValidationError(
                $"Shipment {shipment.ShipmentNumber} cannot be delivered from status '{shipment.Status}'."));

        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShipmentDto>.Success(shipment.ToDto());
    }
}
