using Ardalis.Result;
using ModularShop.Kernel.Application.Abstractions;
using ModularShop.Kernel.Domain.Repositories;
using ModularShop.Modules.Support.Application.Dtos;
using ModularShop.Modules.Support.Application.Mappings;
using ModularShop.Modules.Support.Domain;

namespace ModularShop.Modules.Support.Application.UseCases;

/// <summary>Use case: move a ticket to a new status (Open / Pending / Resolved / Closed).</summary>
public sealed class ChangeTicketStatusUseCase : UseCase
{
    private readonly IReadRepository<Ticket> _tickets;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeTicketStatusUseCase(IReadRepository<Ticket> tickets, IUnitOfWork unitOfWork)
    {
        _tickets = tickets;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TicketDto>> ExecuteAsync(Guid ticketId, ChangeStatusRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<TicketStatus>(request.Status, ignoreCase: true, out var status))
            return Result<TicketDto>.Invalid(new ValidationError($"'{request.Status}' is not a valid ticket status."));

        var ticket = await _tickets.GetForUpdateAsync(t => t.Id == ticketId, ct, t => t.Messages); // tracked
        if (ticket is null)
            return Result<TicketDto>.NotFound($"Ticket {ticketId} was not found.");

        ticket.ChangeStatus(status);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<TicketDto>.Success(ticket.ToDto());
    }
}
