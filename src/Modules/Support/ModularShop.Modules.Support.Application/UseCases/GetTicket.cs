using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Support.Domain;

namespace ModularShop.Modules.Support.Application;

/// <summary>Use case: fetch a single ticket with its full message thread.</summary>
public sealed class GetTicket
{
    private readonly DbContext _db;

    public GetTicket(DbContext db) => _db = db;

    public async Task<Result<TicketDto>> ExecuteAsync(Guid id, CancellationToken ct)
    {
        var ticket = await _db.Set<Ticket>()
            .Include(t => t.Messages)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        return ticket is null
            ? Result<TicketDto>.NotFound($"Ticket {id} was not found.")
            : Result<TicketDto>.Success(ticket.ToDto());
    }
}
