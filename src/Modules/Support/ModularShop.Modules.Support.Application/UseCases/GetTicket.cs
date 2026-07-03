using Ardalis.Result;
using ModularShop.Kernel.Domain.Repositories;
using ModularShop.Modules.Support.Domain;

namespace ModularShop.Modules.Support.Application;

/// <summary>Use case: fetch a single ticket with its full message thread.</summary>
public sealed class GetTicket
{
    private readonly IReadRepository<Ticket> _tickets;

    public GetTicket(IReadRepository<Ticket> tickets) => _tickets = tickets;

    public async Task<Result<TicketDto>> ExecuteAsync(Guid id, CancellationToken ct)
    {
        var ticket = await _tickets.GetWithIncludesAsync(t => t.Id == id, ct, t => t.Messages);

        return ticket is null
            ? Result<TicketDto>.NotFound($"Ticket {id} was not found.")
            : Result<TicketDto>.Success(ticket.ToDto());
    }
}
