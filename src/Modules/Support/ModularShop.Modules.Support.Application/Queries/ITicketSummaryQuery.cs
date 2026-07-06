using ModularShop.Modules.Support.Domain;

namespace ModularShop.Modules.Support.Application.Queries;

/// <summary>
/// A lightweight projection of a <see cref="Ticket"/> for list views: the header fields plus a message
/// count. Deliberately NOT the full ticket graph — the list does not need message bodies, only how many
/// there are. Produced by <see cref="ITicketSummaryQuery.ListAsync"/>.
/// </summary>
public sealed record TicketSummary(
    Guid Id,
    string Subject,
    string CustomerName,
    TicketStatus Status,
    DateTime CreatedOnUtc,
    int MessageCount);

/// <summary>
/// A read-only query — deliberately NOT a repository. A repository's job is to reconstitute
/// <see cref="Ticket"/> aggregates; this returns a report-shaped projection the repository abstraction
/// has no business returning. Implemented in Infrastructure, where the EF projection lives.
/// </summary>
public interface ITicketSummaryQuery
{
    /// <summary>Lists ticket summaries (header + message count), most recently opened first.</summary>
    Task<IReadOnlyList<TicketSummary>> ListAsync(CancellationToken cancellationToken = default);
}
