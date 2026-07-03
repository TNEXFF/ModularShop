using ModularShop.Kernel.Domain;

namespace ModularShop.Modules.Support.Domain;

/// <summary>A message on a support <see cref="Ticket"/>, written by a user (customer or agent).</summary>
public sealed class TicketMessage : Entity
{
    public Guid TicketId { get; private set; }
    public string AuthorUserId { get; private set; } = default!;
    public string AuthorName { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public DateTime SentOnUtc { get; private set; }

    private TicketMessage() { } // EF

    public TicketMessage(Guid id, Guid ticketId, string authorUserId, string authorName, string body, DateTime sentOnUtc)
        : base(id)
    {
        TicketId = ticketId;
        AuthorUserId = authorUserId;
        AuthorName = authorName;
        Body = body;
        SentOnUtc = sentOnUtc;
    }
}
