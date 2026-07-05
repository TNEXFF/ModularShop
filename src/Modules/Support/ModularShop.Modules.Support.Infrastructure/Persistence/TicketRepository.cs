using Microsoft.EntityFrameworkCore;
using ModularShop.Kernel.Infrastructure.Persistence.Repositories;
using ModularShop.Modules.Support.Domain;
using ModularShop.Modules.Support.Domain.Repositories;

namespace ModularShop.Modules.Support.Infrastructure.Persistence;

/// <summary>
/// Support's specific repository. It subclasses the kernel's generic <see cref="Repository{T}"/> (which
/// is why that base class is public) to inherit all the standard <see cref="Ticket"/> operations, and
/// implements only the one query that needs a hand-written shape.
/// </summary>
internal sealed class TicketRepository : Repository<Ticket>, ITicketRepository
{
    public TicketRepository(DbContext context) : base(context) { }

    public async Task<IReadOnlyList<TicketSummary>> ListSummariesAsync(CancellationToken cancellationToken = default)
        // EF Core translates `t.Messages.Count` into a correlated COUNT sub-query, so the message bodies
        // are never fetched. Filtering, ordering and projection all run in the database — plain LINQ, no
        // raw SQL. This is what the generic ListWithIncludes(t => t.Messages) could not do efficiently.
        => await Set.AsNoTracking()
            .OrderByDescending(t => t.CreatedOnUtc)
            .Select(t => new TicketSummary(
                t.Id, t.Subject, t.CustomerName, t.Status, t.CreatedOnUtc, t.Messages.Count))
            .ToListAsync(cancellationToken);
}
