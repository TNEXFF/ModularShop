using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Modules.Shipping.Domain;
using ModularShop.Modules.Shipping.Infrastructure.Persistence;

namespace ModularShop.Modules.Shipping.Infrastructure;

/// <summary>Migrates the <c>shipping</c> schema and seeds historical shipments on startup.</summary>
internal sealed class ShippingModuleInitializer : IModuleInitializer
{
    private readonly ShippingDbContext _db;
    private readonly ILogger<ShippingModuleInitializer> _logger;

    public ShippingModuleInitializer(ShippingDbContext db, ILogger<ShippingModuleInitializer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _db.Database.MigrateAsync(cancellationToken);

        if (await _db.Shipments.AnyAsync(cancellationToken))
            return;

        _db.Shipments.AddRange(ShippingSeed.Shipments());
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded Shipping shipments.");
    }
}

/// <summary>
/// Seed data for Shipping (shipment ids use the prefix <c>40000000-…</c>). These deliberately mirror
/// the historical orders seeded by the Sales module (order ids <c>30000000-…</c>) so the demo data is
/// coherent. In normal operation Shipping learns about an order ONLY through the <c>OrderPlaced</c>
/// event — this seed is a one-off for a populated first-run experience.
/// </summary>
internal static class ShippingSeed
{
    private static Guid S(int n) => new($"40000000-0000-0000-0000-0000000000{n:D2}");
    private static Guid O(int n) => new($"30000000-0000-0000-0000-0000000000{n:D2}");

    public static IReadOnlyList<Shipment> Shipments()
    {
        var now = DateTime.UtcNow;
        var list = new List<Shipment>();

        var s1 = new Shipment(S(1), "SHP-1001", O(1), "ORD-1001", "Alice Johnson", now.AddDays(-24));
        s1.AddItem("Mechanical Keyboard", 1);
        s1.AddItem("Ergonomic Wireless Mouse", 1);
        s1.ApplySeedState(ShipmentStatus.Delivered, "UPS", "1Z100200300", now.AddDays(-23), now.AddDays(-21));
        list.Add(s1);

        var s2 = new Shipment(S(2), "SHP-1002", O(2), "ORD-1002", "Carla Nguyen", now.AddDays(-18));
        s2.AddItem("27\" 4K Monitor", 2);
        s2.ApplySeedState(ShipmentStatus.Delivered, "FedEx", "FX558877112", now.AddDays(-17), now.AddDays(-15));
        list.Add(s2);

        var s3 = new Shipment(S(3), "SHP-1003", O(3), "ORD-1003", "Emma Wilson", now.AddDays(-11));
        s3.AddItem("Noise-Cancelling Headset", 1);
        s3.AddItem("65W USB-C Charger", 2);
        s3.ApplySeedState(ShipmentStatus.Shipped, "DHL Express", "1Z900900900", now.AddDays(-9), null);
        list.Add(s3);

        var s4 = new Shipment(S(4), "SHP-1004", O(4), "ORD-1004", "Bob Martinez", now.AddDays(-7));
        s4.AddItem("1TB NVMe SSD", 2);
        s4.AddItem("2TB NVMe SSD", 1);
        s4.ApplySeedState(ShipmentStatus.Delivered, "UPS", "1Z777888999", now.AddDays(-6), now.AddDays(-4));
        list.Add(s4);

        var s5 = new Shipment(S(5), "SHP-1005", O(5), "ORD-1005", "George Brown", now.AddDays(-3));
        s5.AddItem("USB-C Docking Station", 1);
        s5.AddItem("Wi-Fi 6 Router", 1);
        s5.ApplySeedState(ShipmentStatus.Shipped, "FedEx", "FX223344556", now.AddDays(-2), null);
        list.Add(s5);

        // Most recent order is still awaiting dispatch.
        var s6 = new Shipment(S(6), "SHP-1006", O(6), "ORD-1006", "Ivan Petrov", now.AddDays(-1));
        s6.AddItem("34\" Ultrawide Monitor", 1);
        list.Add(s6);

        return list;
    }
}
