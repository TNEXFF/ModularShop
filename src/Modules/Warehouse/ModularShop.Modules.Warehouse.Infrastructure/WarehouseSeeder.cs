using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Modules.Warehouse.Domain;

namespace ModularShop.Modules.Warehouse.Infrastructure;

/// <summary>
/// Seeds the product catalogue on startup, through the shared host <see cref="DbContext"/>. The host has
/// already migrated the single database and the kernel has already seeded the currencies these products
/// reference.
/// </summary>
internal sealed class WarehouseSeeder : IModuleInitializer
{
    private readonly DbContext _db;
    private readonly ILogger<WarehouseSeeder> _logger;

    public WarehouseSeeder(DbContext db, ILogger<WarehouseSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.Set<Product>().AnyAsync(cancellationToken))
            return;

        _db.Set<Product>().AddRange(WarehouseSeed.Products());
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded Warehouse catalogue ({Count} products).",
            await _db.Set<Product>().CountAsync(cancellationToken));
    }
}

/// <summary>
/// Seed data for the catalogue. Product ids are fixed GUIDs (prefix <c>10000000-…</c>) so the demo data
/// is stable across runs. Most products are priced in USD; a few use EUR/GBP to show the shared kernel
/// <c>Currency</c> in action across the Warehouse and Sales modules.
/// </summary>
internal static class WarehouseSeed
{
    private static Guid P(int n) => new($"10000000-0000-0000-0000-0000000000{n:D2}");

    public static IReadOnlyList<Product> Products() =>
    [
        new(P(1),  "KB-MECH-01", "Mechanical Keyboard",       "Hot-swappable tactile mechanical keyboard, RGB backlit.", "Peripherals", 89.99m, 140),
        new(P(2),  "MS-ERGO-02", "Ergonomic Wireless Mouse",  "Vertical ergonomic mouse with silent clicks.",            "Peripherals", 39.50m, 220),
        new(P(3),  "MS-GAME-03", "Gaming Mouse",              "16000 DPI optical gaming mouse.",                         "Peripherals", 54.00m, 95),
        new(P(4),  "MON-27-4K",  "27\" 4K Monitor",           "27-inch 4K UHD IPS monitor, USB-C 90W.",                  "Displays",    329.00m, 60,  "EUR"),
        new(P(5),  "MON-34-UW",  "34\" Ultrawide Monitor",    "34-inch curved ultrawide, 144Hz.",                        "Displays",    549.00m, 28,  "EUR"),
        new(P(6),  "HS-NC-05",   "Noise-Cancelling Headset",  "Over-ear ANC headset with boom mic.",                     "Audio",       149.99m, 80),
        new(P(7),  "SPK-BT-06",  "Bluetooth Speaker",         "Portable splash-proof Bluetooth speaker.",                "Audio",       59.99m, 130),
        new(P(8),  "WBC-1080-7", "1080p Webcam",              "Full-HD webcam with privacy shutter.",                    "Peripherals", 45.00m, 110),
        new(P(9),  "DOCK-USBC",  "USB-C Docking Station",     "11-in-1 USB-C dock, dual 4K output.",                     "Accessories", 119.00m, 70),
        new(P(10), "SSD-1TB-10", "1TB NVMe SSD",              "PCIe Gen4 NVMe SSD, 7000 MB/s.",                          "Storage",     94.99m, 200),
        new(P(11), "SSD-2TB-11", "2TB NVMe SSD",              "PCIe Gen4 NVMe SSD, 7000 MB/s.",                          "Storage",     169.99m, 90),
        new(P(12), "HDD-4TB-12", "4TB External HDD",          "USB 3.2 desktop external hard drive.",                    "Storage",     104.00m, 65),
        new(P(13), "RTR-WIFI6",  "Wi-Fi 6 Router",            "Dual-band AX3000 Wi-Fi 6 router.",                        "Networking",  99.00m, 75,  "GBP"),
        new(P(14), "SW-8P-14",   "8-Port Gigabit Switch",     "Unmanaged 8-port gigabit ethernet switch.",               "Networking",  32.99m, 160),
        new(P(15), "CHR-65W-15", "65W USB-C Charger",         "GaN 65W USB-C fast charger.",                             "Accessories", 29.99m, 300),
        new(P(16), "HUB-USB-16", "4-Port USB Hub",            "Powered 4-port USB 3.0 hub.",                             "Accessories", 19.99m, 260),
        new(P(17), "CAM-4K-17",  "4K Action Camera",          "Waterproof 4K action camera with stabilisation.",         "Audio",       179.00m, 40,  "EUR"),
        new(P(18), "STND-MON18", "Monitor Arm",               "Gas-spring single-monitor desk arm.",                     "Accessories", 49.99m, 85),
    ];
}
