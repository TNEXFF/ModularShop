using ModularShop.Kernel.Domain;

namespace ModularShop.Modules.Support.Domain;

/// <summary>A message on a support <see cref="Ticket"/>, written by a user (customer or agent).
/// <see cref="TicketId"/> is set by EF Core from the owning ticket's navigation when the graph is saved.</summary>
public sealed class TicketMessage : Entity
{
    public Guid TicketId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public string AuthorName { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public DateTime SentOnUtc { get; private set; }

    private TicketMessage() { } // EF

    public TicketMessage(Guid authorUserId, string authorName, string body, DateTime sentOnUtc)
    {
        AuthorUserId = authorUserId;
        AuthorName = authorName;
        Body = body;
        SentOnUtc = sentOnUtc;
    }
}
