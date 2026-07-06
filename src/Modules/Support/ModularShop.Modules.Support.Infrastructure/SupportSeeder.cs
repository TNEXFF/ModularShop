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
/// Seed data for Support (ticket ids use the prefix <c>50000000-…</c>, passed through the constructor's
/// optional <c>id</c>). Tickets reference the same kernel customer ids (<c>20000000-…</c>) as the
/// orders/shipments, so a customer's orders and support history line up. Seeded messages are stamped with
/// <see cref="SeedUser"/> (the empty Guid) for authorship, since no interactive user exists at seed time.
/// </summary>
internal static class SupportSeed
{
    private static Guid C(int n) => new($"20000000-0000-0000-0000-0000000000{n:D2}");
    private static Guid T(int n) => new($"50000000-0000-0000-0000-0000000000{n:D2}");

    /// <summary>Authorship stamp for seeded tickets/messages — the empty Guid stands for "system/seed".</summary>
    private static readonly Guid SeedUser = Guid.Empty;

    public static IReadOnlyList<Ticket> Tickets()
    {
        var now = DateTime.UtcNow;
        var list = new List<Ticket>();

        var t1 = new Ticket("Keyboard key intermittently not registering",
            "The 'K' key on my new mechanical keyboard only registers about half the time.",
            C(1), "Alice Johnson", SeedUser, "Alice Johnson", now.AddDays(-5), id: T(1));
        t1.AddMessage(SeedUser, "Sam Agent", "Thanks Alice — could you share the SKU printed on the base of the keyboard?",
            now.AddDays(-5).AddHours(3));
        t1.ApplySeedState(TicketStatus.Pending, null);
        list.Add(t1);

        var t2 = new Ticket("Monitor arrived with a stuck pixel",
            "There's a stuck red pixel near the centre of the 4K monitor I received.",
            C(3), "Carla Nguyen", SeedUser, "Carla Nguyen", now.AddDays(-3), id: T(2));
        t2.AddMessage(SeedUser, "Sam Agent", "Sorry about that — we've approved a replacement and it ships today.",
            now.AddDays(-3).AddHours(2));
        t2.AddMessage(SeedUser, "Carla Nguyen", "Brilliant, thank you!", now.AddDays(-3).AddHours(20));
        t2.ApplySeedState(TicketStatus.Resolved, now.AddDays(-2));
        list.Add(t2);

        var t3 = new Ticket("Will the USB-C dock drive two 4K displays?",
            "I'd like to run two 4K monitors at 60Hz from the USB-C docking station — is that supported?",
            C(7), "George Brown", SeedUser, "George Brown", now.AddDays(-1), id: T(3));
        list.Add(t3);

        // A closed ticket — demonstrates the TicketStatus.Closed state, with a conversation that predates
        // the close time.
        var t4 = new Ticket("Refund for returned webcam not yet received",
            "I returned the 1080p webcam two weeks ago but haven't seen the refund on my card.",
            C(8), "Hana Suzuki", SeedUser, "Hana Suzuki", now.AddDays(-6), id: T(4));
        t4.AddMessage(SeedUser, "Sam Agent", "Thanks Hana — the return is logged and the refund has now been issued.",
            now.AddDays(-6).AddHours(5));
        t4.AddMessage(SeedUser, "Hana Suzuki", "Confirmed, the refund just arrived. Thank you!", now.AddDays(-5));
        t4.ApplySeedState(TicketStatus.Closed, now.AddDays(-4));
        list.Add(t4);

        return list;
    }
}
