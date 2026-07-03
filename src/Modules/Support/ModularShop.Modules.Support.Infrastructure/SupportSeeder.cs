using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Modules.Support.Domain;

namespace ModularShop.Modules.Support.Infrastructure;

/// <summary>Seeds a few support tickets on startup, through the shared host <see cref="DbContext"/>.</summary>
internal sealed class SupportSeeder : IModuleInitializer
{
    private readonly DbContext _db;
    private readonly ILogger<SupportSeeder> _logger;

    public SupportSeeder(DbContext db, ILogger<SupportSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.Set<Ticket>().AnyAsync(cancellationToken))
            return;

        _db.Set<Ticket>().AddRange(SupportSeed.Tickets());
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded Support tickets.");
    }
}

/// <summary>
/// Seed data for Support (ticket ids use the prefix <c>50000000-…</c>). Tickets reference the same kernel
/// customer ids (<c>20000000-…</c>) as the orders/shipments, so a customer's orders and support history
/// line up.
/// </summary>
internal static class SupportSeed
{
    private static Guid C(int n) => new($"20000000-0000-0000-0000-0000000000{n:D2}");
    private static Guid T(int n) => new($"50000000-0000-0000-0000-0000000000{n:D2}");

    public static IReadOnlyList<Ticket> Tickets()
    {
        var now = DateTime.UtcNow;
        var list = new List<Ticket>();

        var t1 = new Ticket(T(1), "Keyboard key intermittently not registering",
            "The 'K' key on my new mechanical keyboard only registers about half the time.",
            C(1), "Alice Johnson", "seed", "Alice Johnson", now.AddDays(-5));
        t1.AddMessage("seed", "Sam Agent", "Thanks Alice — could you share the SKU printed on the base of the keyboard?");
        t1.ApplySeedState(TicketStatus.Pending, null);
        list.Add(t1);

        var t2 = new Ticket(T(2), "Monitor arrived with a stuck pixel",
            "There's a stuck red pixel near the centre of the 4K monitor I received.",
            C(3), "Carla Nguyen", "seed", "Carla Nguyen", now.AddDays(-3));
        t2.AddMessage("seed", "Sam Agent", "Sorry about that — we've approved a replacement and it ships today.");
        t2.AddMessage("seed", "Carla Nguyen", "Brilliant, thank you!");
        t2.ApplySeedState(TicketStatus.Resolved, now.AddDays(-2));
        list.Add(t2);

        var t3 = new Ticket(T(3), "Will the USB-C dock drive two 4K displays?",
            "I'd like to run two 4K monitors at 60Hz from the USB-C docking station — is that supported?",
            C(7), "George Brown", "seed", "George Brown", now.AddDays(-1));
        list.Add(t3);

        return list;
    }
}
