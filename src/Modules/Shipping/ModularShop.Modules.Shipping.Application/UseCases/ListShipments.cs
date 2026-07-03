using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Shipping.Domain;

namespace ModularShop.Modules.Shipping.Application;

/// <summary>Use case: list all shipments (most recent first), each with its items.</summary>
public sealed class ListShipments
{
    private readonly DbContext _db;

    public ListShipments(DbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<ShipmentDto>>> ExecuteAsync(CancellationToken ct)
    {
        var shipments = await _db.Set<Shipment>()
            .Include(s => s.Items)
            .OrderByDescending(s => s.CreatedOnUtc)
            .AsNoTracking()
            .ToListAsync(ct);

        return Result<IReadOnlyList<ShipmentDto>>.Success(shipments.Select(s => s.ToDto()).ToList());
    }
}
