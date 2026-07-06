using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Support.Application.Queries;
using ModularShop.Modules.Support.Domain;

namespace ModularShop.Modules.Support.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="ITicketSummaryQuery"/>. Not a repository — it runs directly
/// against the base <see cref="DbContext"/> (the one host context) rather than through <c>Repository&lt;T&gt;</c>.
/// </summary>
internal sealed class TicketSummaryQuery(DbContext context) : ITicketSummaryQuery
{
    public async Task<IReadOnlyList<TicketSummary>> ListAsync(CancellationToken cancellationToken = default)
        // EF Core translates `t.Messages.Count` into a correlated COUNT sub-query, so the message bodies
        // are never fetched. Filtering, ordering and projection all run in the database — plain LINQ, no
        // raw SQL.
        => await context.Set<Ticket>().AsNoTracking()
            .OrderByDescending(t => t.CreatedOnUtc)
            .Select(t => new TicketSummary(
                t.Id, t.Subject, t.CustomerName, t.Status, t.CreatedOnUtc, t.Messages.Count))
            .ToListAsync(cancellationToken);
}
