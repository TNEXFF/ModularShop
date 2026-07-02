using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Modules.Sales.Domain;
using ModularShop.Modules.Sales.Infrastructure.Persistence;

namespace ModularShop.Modules.Sales.Infrastructure;

/// <summary>Migrates the <c>sales</c> schema and seeds customers + historical orders on startup.</summary>
internal sealed class SalesModuleInitializer : IModuleInitializer
{
    private readonly SalesDbContext _db;
    private readonly ILogger<SalesModuleInitializer> _logger;

    public SalesModuleInitializer(SalesDbContext db, ILogger<SalesModuleInitializer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _db.Database.MigrateAsync(cancellationToken);

        if (await _db.Customers.AnyAsync(cancellationToken))
            return;

        _db.Customers.AddRange(SalesSeed.Customers());
        _db.Orders.AddRange(SalesSeed.Orders());
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded Sales customers and historical orders.");
    }
}

/// <summary>
/// Seed data for Sales. Customer ids use the prefix <c>20000000-…</c> and order ids <c>30000000-…</c>.
/// Historical order lines reference Warehouse product ids (<c>10000000-…</c>) with the same
/// names/prices as the Warehouse seed, so the demo data is coherent.
/// </summary>
internal static class SalesSeed
{
    private static Guid C(int n) => new($"20000000-0000-0000-0000-0000000000{n:D2}");
    private static Guid O(int n) => new($"30000000-0000-0000-0000-0000000000{n:D2}");
    private static Guid P(int n) => new($"10000000-0000-0000-0000-0000000000{n:D2}");

    public static IReadOnlyList<Customer> Customers() =>
    [
        new(C(1),  "Alice Johnson", "alice.johnson@contoso.com"),
        new(C(2),  "Bob Martinez",  "bob.martinez@contoso.com"),
        new(C(3),  "Carla Nguyen",  "carla.nguyen@fabrikam.com"),
        new(C(4),  "David Smith",   "david.smith@fabrikam.com"),
        new(C(5),  "Emma Wilson",   "emma.wilson@northwind.com"),
        new(C(6),  "Farah Khan",    "farah.khan@northwind.com"),
        new(C(7),  "George Brown",  "george.brown@adventure-works.com"),
        new(C(8),  "Hana Suzuki",   "hana.suzuki@adventure-works.com"),
        new(C(9),  "Ivan Petrov",   "ivan.petrov@contoso.com"),
        new(C(10), "Julia Rossi",   "julia.rossi@fabrikam.com"),
    ];

    public static IReadOnlyList<Order> Orders()
    {
        var now = DateTime.UtcNow;
        var orders = new List<Order>();

        var o1 = new Order(O(1), "ORD-1001", C(1), "Alice Johnson", "seed", now.AddDays(-24));
        o1.AddLine(P(1), "Mechanical Keyboard", 89.99m, 1);
        o1.AddLine(P(2), "Ergonomic Wireless Mouse", 39.50m, 1);
        o1.MarkCompleted();
        orders.Add(o1);

        var o2 = new Order(O(2), "ORD-1002", C(3), "Carla Nguyen", "seed", now.AddDays(-18));
        o2.AddLine(P(4), "27\" 4K Monitor", 329.00m, 2);
        o2.MarkCompleted();
        orders.Add(o2);

        var o3 = new Order(O(3), "ORD-1003", C(5), "Emma Wilson", "seed", now.AddDays(-11));
        o3.AddLine(P(6), "Noise-Cancelling Headset", 149.99m, 1);
        o3.AddLine(P(15), "65W USB-C Charger", 29.99m, 2);
        orders.Add(o3);

        var o4 = new Order(O(4), "ORD-1004", C(2), "Bob Martinez", "seed", now.AddDays(-7));
        o4.AddLine(P(10), "1TB NVMe SSD", 94.99m, 2);
        o4.AddLine(P(11), "2TB NVMe SSD", 169.99m, 1);
        o4.MarkCompleted();
        orders.Add(o4);

        var o5 = new Order(O(5), "ORD-1005", C(7), "George Brown", "seed", now.AddDays(-3));
        o5.AddLine(P(9), "USB-C Docking Station", 119.00m, 1);
        o5.AddLine(P(13), "Wi-Fi 6 Router", 99.00m, 1);
        orders.Add(o5);

        var o6 = new Order(O(6), "ORD-1006", C(9), "Ivan Petrov", "seed", now.AddDays(-1));
        o6.AddLine(P(5), "34\" Ultrawide Monitor", 549.00m, 1);
        orders.Add(o6);

        return orders;
    }
}
