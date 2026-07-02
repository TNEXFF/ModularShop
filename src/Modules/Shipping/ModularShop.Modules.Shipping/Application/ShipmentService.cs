using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Shipping.Domain;
using ModularShop.Modules.Shipping.Infrastructure;
using ModularShop.SharedKernel.Domain;

namespace ModularShop.Modules.Shipping.Application;

/// <summary>Application service for viewing and advancing shipments.</summary>
internal sealed class ShipmentService
{
    private readonly ShippingDbContext _db;

    public ShipmentService(ShippingDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<ShipmentDto>>> GetShipmentsAsync(CancellationToken ct)
    {
        var shipments = await _db.Shipments.AsNoTracking()
            .Include(s => s.Items)
            .OrderByDescending(s => s.CreatedOnUtc)
            .ToListAsync(ct);
        return Result<IReadOnlyList<ShipmentDto>>.Success(shipments.Select(s => s.ToDto()).ToList());
    }

    public async Task<Result<ShipmentDto>> GetShipmentAsync(Guid id, CancellationToken ct)
    {
        var shipment = await _db.Shipments.AsNoTracking().Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == id, ct);
        return shipment is null
            ? Result<ShipmentDto>.NotFound($"Shipment {id} was not found.")
            : Result<ShipmentDto>.Success(shipment.ToDto());
    }

    public async Task<Result<ShipmentDto>> ShipAsync(Guid id, CancellationToken ct)
    {
        var shipment = await _db.Shipments.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == id, ct);
        if (shipment is null)
            return Result<ShipmentDto>.NotFound($"Shipment {id} was not found.");
        if (!shipment.Ship("DHL Express", GenerateTrackingNumber()))
            return Result<ShipmentDto>.Invalid($"Shipment {shipment.ShipmentNumber} cannot be shipped from status '{shipment.Status}'.");

        await _db.SaveChangesAsync(ct);
        return Result<ShipmentDto>.Success(shipment.ToDto());
    }

    public async Task<Result<ShipmentDto>> DeliverAsync(Guid id, CancellationToken ct)
    {
        var shipment = await _db.Shipments.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == id, ct);
        if (shipment is null)
            return Result<ShipmentDto>.NotFound($"Shipment {id} was not found.");
        if (!shipment.Deliver())
            return Result<ShipmentDto>.Invalid($"Shipment {shipment.ShipmentNumber} cannot be delivered from status '{shipment.Status}'.");

        await _db.SaveChangesAsync(ct);
        return Result<ShipmentDto>.Success(shipment.ToDto());
    }

    private static string GenerateTrackingNumber() => $"1Z{Random.Shared.Next(100000, 999999)}{Random.Shared.Next(100, 999)}";
}

internal static class ShippingMappings
{
    public static ShipmentDto ToDto(this Shipment s) => new(
        s.Id, s.ShipmentNumber, s.OrderId, s.OrderNumber, s.CustomerName, s.Status.ToString(),
        s.CreatedOnUtc, s.ShippedOnUtc, s.DeliveredOnUtc, s.Carrier, s.TrackingNumber, s.TotalUnits,
        s.Items.Select(i => new ShipmentItemDto(i.ProductName, i.Quantity)).ToList());
}
