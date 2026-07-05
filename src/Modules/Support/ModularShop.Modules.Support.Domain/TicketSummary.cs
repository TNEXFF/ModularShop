using ModularShop.Modules.Support.Domain.Repositories;

namespace ModularShop.Modules.Support.Domain;

/// <summary>
/// A lightweight projection of a <see cref="Ticket"/> for list views: the header fields plus a message
/// count. Deliberately NOT the full ticket graph — the list does not need message bodies, only how many
/// there are. Produced by <see cref="ITicketRepository.ListSummariesAsync"/>.
/// </summary>
public sealed record TicketSummary(
    Guid Id,
    string Subject,
    string CustomerName,
    TicketStatus Status,
    DateTime CreatedOnUtc,
    int MessageCount);
