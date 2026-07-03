using Ardalis.Result;
using ModularShop.Modules.Support.Domain;

namespace ModularShop.Modules.Support.Application;

/// <summary>
/// Use case: list all support tickets (most recently opened first). It uses the module's SPECIFIC
/// <see cref="ITicketRepository"/> rather than the generic repository, because the list needs a message
/// count per ticket — which the specific repository projects in the database instead of loading every
/// message body (see <see cref="ITicketRepository.ListSummariesAsync"/>).
/// </summary>
public sealed class ListTickets
{
    private readonly ITicketRepository _tickets;

    public ListTickets(ITicketRepository tickets) => _tickets = tickets;

    public async Task<Result<IReadOnlyList<TicketListItemDto>>> ExecuteAsync(CancellationToken ct)
    {
        var summaries = await _tickets.ListSummariesAsync(ct);
        return Result<IReadOnlyList<TicketListItemDto>>.Success(summaries.Select(s => s.ToListItem()).ToList());
    }
}
