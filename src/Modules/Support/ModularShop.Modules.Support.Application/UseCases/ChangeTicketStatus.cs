using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Support.Domain;

namespace ModularShop.Modules.Support.Application;

/// <summary>Use case: move a ticket to a new status (Open / Pending / Resolved / Closed).</summary>
public sealed class ChangeTicketStatus
{
    private readonly DbContext _db;

    public ChangeTicketStatus(DbContext db) => _db = db;

    public async Task<Result<TicketDto>> ExecuteAsync(Guid ticketId, ChangeStatusRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<TicketStatus>(request.Status, ignoreCase: true, out var status))
            return Result<TicketDto>.Invalid(new ValidationError($"'{request.Status}' is not a valid ticket status."));

        var ticket = await _db.Set<Ticket>()
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket is null)
            return Result<TicketDto>.NotFound($"Ticket {ticketId} was not found.");

        ticket.ChangeStatus(status);
        await _db.SaveChangesAsync(ct);

        return Result<TicketDto>.Success(ticket.ToDto());
    }
}
