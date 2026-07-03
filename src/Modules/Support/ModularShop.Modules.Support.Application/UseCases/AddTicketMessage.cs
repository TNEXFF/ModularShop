using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using ModularShop.Kernel.Application;
using ModularShop.Modules.Support.Domain;

namespace ModularShop.Modules.Support.Application;

/// <summary>Use case: add a message to a ticket's thread, authored by the current user.</summary>
public sealed class AddTicketMessage
{
    private readonly DbContext _db;
    private readonly ICurrentUser _currentUser;

    public AddTicketMessage(DbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<TicketDto>> ExecuteAsync(Guid ticketId, AddMessageRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
            return Result<TicketDto>.Invalid(new ValidationError("A message cannot be empty."));

        var ticket = await _db.Set<Ticket>()
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct); // tracked — the new message is persisted
        if (ticket is null)
            return Result<TicketDto>.NotFound($"Ticket {ticketId} was not found.");

        ticket.AddMessage(_currentUser.UserId, _currentUser.UserName, request.Body.Trim());
        await _db.SaveChangesAsync(ct);

        return Result<TicketDto>.Success(ticket.ToDto());
    }
}
