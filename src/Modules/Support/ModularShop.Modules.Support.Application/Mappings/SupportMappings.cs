using ModularShop.Modules.Support.Domain;

namespace ModularShop.Modules.Support.Application;

internal static class SupportMappings
{
    public static TicketDto ToDto(this Ticket t) => new(
        t.Id, t.Subject, t.Description, t.CustomerId, t.CustomerName, t.Status.ToString(),
        t.CreatedByName, t.CreatedOnUtc, t.ResolvedOnUtc,
        t.Messages
            .OrderBy(m => m.SentOnUtc)
            .Select(m => new TicketMessageDto(m.AuthorName, m.Body, m.SentOnUtc))
            .ToList());

    public static TicketListItemDto ToListItem(this Ticket t) =>
        new(t.Id, t.Subject, t.CustomerName, t.Status.ToString(), t.CreatedOnUtc, t.Messages.Count);
}
