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
/// Seed data for the catalogue. Product ids are fixed GUIDs (prefix <c>10000000-…</c>) passed through the
/// constructor's optional <c>id</c> so the demo data is stable across runs; EF honours the explicit key.
/// Everything is priced in USD — the single seeded <c>Currency</c> — which both Warehouse and Sales
/// reference through the shared kernel.
/// </summary>
internal static class WarehouseSeed
{
    private static Guid P(int n) => new($"10000000-0000-0000-0000-0000000000{n:D2}");

    // Opening stock below is already NET of the units consumed by the 6 historical orders that Sales seeds
    // (see SalesSeed.Orders), so the catalogue a learner first sees is consistent with the order history:
    //   ORD-1001 −1 Keyboard, −1 Ergonomic Mouse · ORD-1002 −2 4K Monitor · ORD-1003 −1 Headset, −2 Charger
    //   ORD-1004 −2 1TB SSD, −1 2TB SSD · ORD-1005 −1 Dock, −1 Router · ORD-1006 −1 Ultrawide Monitor
    // (The cancelled ORD-1007 releases its stock, so it consumes nothing.)
    public static IReadOnlyList<Product> Products() =>
    [
        new("KB-MECH-01", "Mechanical Keyboard",       "Hot-swappable tactile mechanical keyboard, RGB backlit.", "Peripherals", 89.99m, 139, id: P(1)),
        new("MS-ERGO-02", "Ergonomic Wireless Mouse",  "Vertical ergonomic mouse with silent clicks.",            "Peripherals", 39.50m, 219, id: P(2)),
        new("MS-GAME-03", "Gaming Mouse",              "16000 DPI optical gaming mouse.",                         "Peripherals", 54.00m, 95,  id: P(3)),
        new("MON-27-4K",  "27\" 4K Monitor",           "27-inch 4K UHD IPS monitor, USB-C 90W.",                  "Displays",    329.00m, 58, id: P(4)),
        new("MON-34-UW",  "34\" Ultrawide Monitor",    "34-inch curved ultrawide, 144Hz.",                        "Displays",    549.00m, 27, id: P(5)),
        new("HS-NC-05",   "Noise-Cancelling Headset",  "Over-ear ANC headset with boom mic.",                     "Audio",       149.99m, 79, id: P(6)),
        new("SPK-BT-06",  "Bluetooth Speaker",         "Portable splash-proof Bluetooth speaker.",                "Audio",       59.99m, 130, id: P(7)),
        new("WBC-1080-7", "1080p Webcam",              "Full-HD webcam with privacy shutter.",                    "Peripherals", 45.00m, 110, id: P(8)),
        new("DOCK-USBC",  "USB-C Docking Station",     "11-in-1 USB-C dock, dual 4K output.",                     "Accessories", 119.00m, 69, id: P(9)),
        new("SSD-1TB-10", "1TB NVMe SSD",              "PCIe Gen4 NVMe SSD, 7000 MB/s.",                          "Storage",     94.99m, 198, id: P(10)),
        new("SSD-2TB-11", "2TB NVMe SSD",              "PCIe Gen4 NVMe SSD, 7000 MB/s.",                          "Storage",     169.99m, 89, id: P(11)),
        new("HDD-4TB-12", "4TB External HDD",          "USB 3.2 desktop external hard drive.",                    "Storage",     104.00m, 65, id: P(12)),
        new("RTR-WIFI6",  "Wi-Fi 6 Router",            "Dual-band AX3000 Wi-Fi 6 router.",                        "Networking",  99.00m, 74, id: P(13)),
        new("SW-8P-14",   "8-Port Gigabit Switch",     "Unmanaged 8-port gigabit ethernet switch.",               "Networking",  32.99m, 160, id: P(14)),
        new("CHR-65W-15", "65W USB-C Charger",         "GaN 65W USB-C fast charger.",                             "Accessories", 29.99m, 298, id: P(15)),
        new("HUB-USB-16", "4-Port USB Hub",            "Powered 4-port USB 3.0 hub.",                             "Accessories", 19.99m, 260, id: P(16)),
        new("CAM-4K-17",  "4K Action Camera",          "Waterproof 4K action camera with stabilisation.",         "Audio",       179.00m, 40, id: P(17)),
        new("STND-MON18", "Monitor Arm",               "Gas-spring single-monitor desk arm.",                     "Accessories", 49.99m, 85, id: P(18)),
    ];
}
