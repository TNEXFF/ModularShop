using ModularShop.Kernel.Domain;

namespace ModularShop.Modules.Support.Domain;

/// <summary>
/// A customer-support ticket (aggregate root), owned by the Support module. Support is a <b>genuinely
/// unrelated</b> module: it plays no part in the order → stock → ship flow and depends on no other
/// module. It does, however, reference the <b>shared kernel</b> <see cref="Customer"/> (a ticket is
/// raised for a customer) and records the Identity user who created it — showing that an independent
/// module still shares the kernel's cross-cutting concerns.
/// </summary>
public sealed class Ticket : Entity
{
    private readonly List<TicketMessage> _messages = new();

    public string Subject { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = default!;
    public TicketStatus Status { get; private set; }
    public string CreatedByUserId { get; private set; } = default!;
    public string CreatedByName { get; private set; } = default!;
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? ResolvedOnUtc { get; private set; }

    public IReadOnlyCollection<TicketMessage> Messages => _messages;

    private Ticket() { } // EF

    public Ticket(Guid id, string subject, string description, Guid customerId, string customerName,
        string createdByUserId, string createdByName, DateTime createdOnUtc)
        : base(id)
    {
        Subject = subject;
        Description = description;
        CustomerId = customerId;
        CustomerName = customerName;
        CreatedByUserId = createdByUserId;
        CreatedByName = createdByName;
        CreatedOnUtc = createdOnUtc;
        Status = TicketStatus.Open;
    }

    public TicketMessage AddMessage(string authorUserId, string authorName, string body)
    {
        var message = new TicketMessage(Guid.NewGuid(), Id, authorUserId, authorName, body, DateTime.UtcNow);
        _messages.Add(message);
        // A reply from anyone re-opens a resolved/closed ticket into Pending — a small, sensible rule.
        if (Status is TicketStatus.Resolved or TicketStatus.Closed)
            Status = TicketStatus.Pending;
        return message;
    }

    public void ChangeStatus(TicketStatus status)
    {
        Status = status;
        ResolvedOnUtc = status is TicketStatus.Resolved or TicketStatus.Closed ? DateTime.UtcNow : null;
    }

    /// <summary>Used only by seeding, to place a historical ticket into a known state.</summary>
    public void ApplySeedState(TicketStatus status, DateTime? resolvedOnUtc)
    {
        Status = status;
        ResolvedOnUtc = resolvedOnUtc;
    }
}
