using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Shipping.Domain;

namespace ModularShop.Modules.Shipping.Application;

/// <summary>Use case: fetch a single shipment (with its items) by id.</summary>
public sealed class GetShipment
{
    private readonly DbContext _db;

    public GetShipment(DbContext db) => _db = db;

    public async Task<Result<ShipmentDto>> ExecuteAsync(Guid id, CancellationToken ct)
    {
        var shipment = await _db.Set<Shipment>()
            .Include(s => s.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        return shipment is null
            ? Result<ShipmentDto>.NotFound($"Shipment {id} was not found.")
            : Result<ShipmentDto>.Success(shipment.ToDto());
    }
}
