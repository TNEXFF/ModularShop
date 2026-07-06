using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Modules.Sales.Domain;

namespace ModularShop.Modules.Sales.Infrastructure;

/// <summary>
/// Seeds historical orders on startup. With centralised migrations the host has already migrated the
/// single database and the kernel has already seeded customers, so this only inserts rows — through the
/// shared host <see cref="DbContext"/>.
/// </summary>
internal sealed class SalesSeeder : IModuleInitializer
{
    private readonly DbContext _db;
    private readonly ILogger<SalesSeeder> _logger;

    public SalesSeeder(DbContext db, ILogger<SalesSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.Set<Order>().AnyAsync(cancellationToken))
            return;

        _db.Set<Order>().AddRange(SalesSeed.Orders());
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded Sales historical orders.");
    }
}

/// <summary>
/// Seed data for Sales. Order ids use the prefix <c>30000000-…</c> (passed through the constructor's
/// optional <c>id</c>) and reference the kernel customer ids (<c>20000000-…</c>) and Warehouse product ids
/// (<c>10000000-…</c>) with matching names/prices, so the demo data is coherent across modules.
/// </summary>
internal static class SalesSeed
{
    private static Guid C(int n) => new($"20000000-0000-0000-0000-0000000000{n:D2}");
    private static Guid O(int n) => new($"30000000-0000-0000-0000-0000000000{n:D2}");
    private static Guid P(int n) => new($"10000000-0000-0000-0000-0000000000{n:D2}");

    public static IReadOnlyList<Order> Orders()
    {
        var now = DateTime.UtcNow;
        var orders = new List<Order>();

        var o1 = new Order("ORD-1001", C(1), "Alice Johnson", "seed", now.AddDays(-24), id: O(1));
        o1.AddLine(P(1), "Mechanical Keyboard", 89.99m, 1);
        o1.AddLine(P(2), "Ergonomic Wireless Mouse", 39.50m, 1);
        o1.MarkCompleted();
        orders.Add(o1);

        var o2 = new Order("ORD-1002", C(3), "Carla Nguyen", "seed", now.AddDays(-18), id: O(2));
        o2.AddLine(P(4), "27\" 4K Monitor", 329.00m, 2);
        o2.MarkCompleted();
        orders.Add(o2);

        var o3 = new Order("ORD-1003", C(5), "Emma Wilson", "seed", now.AddDays(-11), id: O(3));
        o3.AddLine(P(6), "Noise-Cancelling Headset", 149.99m, 1);
        o3.AddLine(P(15), "65W USB-C Charger", 29.99m, 2);
        orders.Add(o3);

        var o4 = new Order("ORD-1004", C(2), "Bob Martinez", "seed", now.AddDays(-7), id: O(4));
        o4.AddLine(P(10), "1TB NVMe SSD", 94.99m, 2);
        o4.AddLine(P(11), "2TB NVMe SSD", 169.99m, 1);
        o4.MarkCompleted();
        orders.Add(o4);

        var o5 = new Order("ORD-1005", C(7), "George Brown", "seed", now.AddDays(-3), id: O(5));
        o5.AddLine(P(9), "USB-C Docking Station", 119.00m, 1);
        o5.AddLine(P(13), "Wi-Fi 6 Router", 99.00m, 1);
        orders.Add(o5);

        var o6 = new Order("ORD-1006", C(9), "Ivan Petrov", "seed", now.AddDays(-1), id: O(6));
        o6.AddLine(P(5), "34\" Ultrawide Monitor", 549.00m, 1);
        orders.Add(o6);

        // A cancelled order — demonstrates the OrderStatus.Cancelled state (its stock was released, so the
        // Warehouse seed does not net it out).
        var o7 = new Order("ORD-1007", C(4), "David Smith", "seed", now.AddDays(-2), id: O(7));
        o7.AddLine(P(3), "Gaming Mouse", 54.00m, 1);
        o7.MarkCancelled();
        orders.Add(o7);

        return orders;
    }
}
