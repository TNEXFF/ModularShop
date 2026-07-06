using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Modules.Shipping.Domain;

namespace ModularShop.Modules.Shipping.Infrastructure;

/// <summary>Seeds historical shipments on startup, through the shared host <see cref="DbContext"/>.</summary>
internal sealed class ShippingSeeder : IModuleInitializer
{
    private readonly DbContext _db;
    private readonly ILogger<ShippingSeeder> _logger;

    public ShippingSeeder(DbContext db, ILogger<ShippingSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.Set<Shipment>().AnyAsync(cancellationToken))
            return;

        _db.Set<Shipment>().AddRange(ShippingSeed.Shipments());
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded Shipping shipments.");
    }
}

/// <summary>
/// Seed data for Shipping (shipment ids use the prefix <c>40000000-…</c>, passed through the constructor's
/// optional <c>id</c>). These mirror the historical orders seeded by Sales (order ids <c>30000000-…</c>)
/// and reference the same kernel customer ids (<c>20000000-…</c>) so the demo data is coherent. In normal
/// operation Shipping learns about an order ONLY through the <c>OrderPlaced</c> event — this seed is a
/// one-off for a populated first run.
/// </summary>
internal static class ShippingSeed
{
    private static Guid S(int n) => new($"40000000-0000-0000-0000-0000000000{n:D2}");
    private static Guid O(int n) => new($"30000000-0000-0000-0000-0000000000{n:D2}");
    private static Guid C(int n) => new($"20000000-0000-0000-0000-0000000000{n:D2}");

    public static IReadOnlyList<Shipment> Shipments()
    {
        var now = DateTime.UtcNow;
        var list = new List<Shipment>();

        var s1 = new Shipment("SHP-1001", O(1), "ORD-1001", C(1), "Alice Johnson", now.AddDays(-24), id: S(1));
        s1.AddItem("Mechanical Keyboard", 1);
        s1.AddItem("Ergonomic Wireless Mouse", 1);
        s1.ApplySeedState(ShipmentStatus.Delivered, "UPS", "1Z100200300", now.AddDays(-23), now.AddDays(-21));
        list.Add(s1);

        var s2 = new Shipment("SHP-1002", O(2), "ORD-1002", C(3), "Carla Nguyen", now.AddDays(-18), id: S(2));
        s2.AddItem("27\" 4K Monitor", 2);
        s2.ApplySeedState(ShipmentStatus.Delivered, "FedEx", "FX558877112", now.AddDays(-17), now.AddDays(-15));
        list.Add(s2);

        var s3 = new Shipment("SHP-1003", O(3), "ORD-1003", C(5), "Emma Wilson", now.AddDays(-11), id: S(3));
        s3.AddItem("Noise-Cancelling Headset", 1);
        s3.AddItem("65W USB-C Charger", 2);
        s3.ApplySeedState(ShipmentStatus.Shipped, "DHL Express", "1Z900900900", now.AddDays(-9), null);
        list.Add(s3);

        var s4 = new Shipment("SHP-1004", O(4), "ORD-1004", C(2), "Bob Martinez", now.AddDays(-7), id: S(4));
        s4.AddItem("1TB NVMe SSD", 2);
        s4.AddItem("2TB NVMe SSD", 1);
        s4.ApplySeedState(ShipmentStatus.Delivered, "UPS", "1Z777888999", now.AddDays(-6), now.AddDays(-4));
        list.Add(s4);

        var s5 = new Shipment("SHP-1005", O(5), "ORD-1005", C(7), "George Brown", now.AddDays(-3), id: S(5));
        s5.AddItem("USB-C Docking Station", 1);
        s5.AddItem("Wi-Fi 6 Router", 1);
        s5.ApplySeedState(ShipmentStatus.Shipped, "FedEx", "FX223344556", now.AddDays(-2), null);
        list.Add(s5);

        // Most recent order is still awaiting dispatch.
        var s6 = new Shipment("SHP-1006", O(6), "ORD-1006", C(9), "Ivan Petrov", now.AddDays(-1), id: S(6));
        s6.AddItem("34\" Ultrawide Monitor", 1);
        list.Add(s6);

        // The dispatch for the cancelled ORD-1007 — demonstrates ShipmentStatus.Cancelled.
        var s7 = new Shipment("SHP-1007", O(7), "ORD-1007", C(4), "David Smith", now.AddDays(-2), id: S(7));
        s7.AddItem("Gaming Mouse", 1);
        s7.ApplySeedState(ShipmentStatus.Cancelled, null, null, null, null);
        list.Add(s7);

        return list;
    }
}
