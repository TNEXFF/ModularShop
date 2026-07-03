using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Shipping.Domain;

namespace ModularShop.Modules.Shipping.Application;

/// <summary>Use case: advance a shipment from Shipped to Delivered.</summary>
public sealed class DeliverShipment
{
    private readonly DbContext _db;

    public DeliverShipment(DbContext db) => _db = db;

    public async Task<Result<ShipmentDto>> ExecuteAsync(Guid id, CancellationToken ct)
    {
        var shipment = await _db.Set<Shipment>()
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, ct); // tracked
        if (shipment is null)
            return Result<ShipmentDto>.NotFound($"Shipment {id} was not found.");

        if (!shipment.Deliver())
            return Result<ShipmentDto>.Invalid(new ValidationError(
                $"Shipment {shipment.ShipmentNumber} cannot be delivered from status '{shipment.Status}'."));

        await _db.SaveChangesAsync(ct);
        return Result<ShipmentDto>.Success(shipment.ToDto());
    }
}
