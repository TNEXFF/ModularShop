namespace ModularShop.Modules.Support.Application.Dtos;

public sealed record TicketMessageDto(string AuthorName, string Body, DateTime SentOnUtc);

public sealed record TicketDto(
    Guid Id,
    string Subject,
    string Description,
    Guid CustomerId,
    string CustomerName,
    string Status,
    string CreatedByName,
    DateTime CreatedOnUtc,
    DateTime? ResolvedOnUtc,
    IReadOnlyList<TicketMessageDto> Messages);

public sealed record TicketListItemDto(
    Guid Id, string Subject, string CustomerName, string Status, DateTime CreatedOnUtc, int MessageCount);

public sealed record CreateTicketRequest(Guid CustomerId, string Subject, string Description);

public sealed record AddMessageRequest(string Body);

public sealed record ChangeStatusRequest(string Status);
