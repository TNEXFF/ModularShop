using Ardalis.Result;
using ModularShop.Modules.Support.Application.Dtos;
using ModularShop.Modules.Support.Application.Mappings;
using ModularShop.Modules.Support.Application.Queries;

namespace ModularShop.Modules.Support.Application.UseCases;

/// <summary>
/// Use case: list all support tickets (most recently opened first). It uses <see cref="ITicketSummaryQuery"/>
/// rather than the repository, because the list needs a message count per ticket — a report-shaped
/// projection the repository (which only returns <c>Ticket</c> entities) has no business producing. The
/// mapping from the query's read model to the API-facing <see cref="TicketListItemDto"/> happens here.
/// </summary>
public sealed class ListTickets
{
    private readonly ITicketSummaryQuery _query;

    public ListTickets(ITicketSummaryQuery query) => _query = query;

    public async Task<Result<IReadOnlyList<TicketListItemDto>>> ExecuteAsync(CancellationToken ct)
    {
        var summaries = await _query.ListAsync(ct);
        return Result<IReadOnlyList<TicketListItemDto>>.Success(summaries.Select(s => s.ToListItem()).ToList());
    }
}
