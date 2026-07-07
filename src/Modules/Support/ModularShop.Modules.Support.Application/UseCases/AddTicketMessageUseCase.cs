using Ardalis.Result;
using ModularShop.Kernel.Application.Abstractions;
using ModularShop.Kernel.Domain.Repositories;
using ModularShop.Modules.Support.Application.Dtos;
using ModularShop.Modules.Support.Application.Mappings;
using ModularShop.Modules.Support.Domain;

namespace ModularShop.Modules.Support.Application.UseCases;

/// <summary>Use case: add a message to a ticket's thread, authored by the current user.</summary>
public sealed class AddTicketMessageUseCase : UseCase
{
    private readonly IReadRepository<Ticket> _tickets;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public AddTicketMessageUseCase(IReadRepository<Ticket> tickets, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _tickets = tickets;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<TicketDto>> ExecuteAsync(Guid ticketId, AddMessageRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
            return Result<TicketDto>.Invalid(new ValidationError("A message cannot be empty."));

        // Tracked (with the existing thread) so the new message is persisted and the DTO shows the full thread.
        var ticket = await _tickets.GetForUpdateAsync(t => t.Id == ticketId, ct, t => t.Messages);
        if (ticket is null)
            return Result<TicketDto>.NotFound($"Ticket {ticketId} was not found.");

        ticket.AddMessage(_currentUser.UserId, _currentUser.UserName, request.Body.Trim());
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<TicketDto>.Success(ticket.ToDto());
    }
}
