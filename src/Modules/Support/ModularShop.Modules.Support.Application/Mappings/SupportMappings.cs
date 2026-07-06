using ModularShop.Modules.Support.Application.Dtos;
using ModularShop.Modules.Support.Application.Queries;
using ModularShop.Modules.Support.Domain;

namespace ModularShop.Modules.Support.Application.Mappings;

internal static class SupportMappings
{
    public static TicketDto ToDto(this Ticket t) => new(
        t.Id, t.Subject, t.Description, t.CustomerId, t.CustomerName, t.Status.ToString(),
        t.CreatedByName, t.CreatedOnUtc, t.ResolvedOnUtc,
        t.Messages
            .OrderBy(m => m.SentOnUtc)
            .Select(m => new TicketMessageDto(m.AuthorName, m.Body, m.SentOnUtc))
            .ToList());

    // Maps the lightweight projection produced by ITicketSummaryQuery.ListAsync (the ticket list
    // does not load full Ticket graphs — only these summary fields + the message count).
    public static TicketListItemDto ToListItem(this TicketSummary s) =>
        new(s.Id, s.Subject, s.CustomerName, s.Status.ToString(), s.CreatedOnUtc, s.MessageCount);
}
