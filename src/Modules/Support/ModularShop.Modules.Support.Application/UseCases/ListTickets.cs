using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Support.Domain;

namespace ModularShop.Modules.Support.Application;

/// <summary>Use case: list all support tickets (most recently opened first).</summary>
public sealed class ListTickets
{
    private readonly DbContext _db;

    public ListTickets(DbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<TicketListItemDto>>> ExecuteAsync(CancellationToken ct)
    {
        var tickets = await _db.Set<Ticket>()
            .Include(t => t.Messages)
            .OrderByDescending(t => t.CreatedOnUtc)
            .AsNoTracking()
            .ToListAsync(ct);

        return Result<IReadOnlyList<TicketListItemDto>>.Success(tickets.Select(t => t.ToListItem()).ToList());
    }
}
