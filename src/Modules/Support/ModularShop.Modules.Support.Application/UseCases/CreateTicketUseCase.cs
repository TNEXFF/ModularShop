using Ardalis.Result;
using Microsoft.Extensions.Logging;
using ModularShop.Kernel.Application;
using ModularShop.Kernel.Application.Abstractions;
using ModularShop.Kernel.Domain;
using ModularShop.Kernel.Domain.Repositories;
using ModularShop.Modules.Support.Application.Dtos;
using ModularShop.Modules.Support.Application.Mappings;
using ModularShop.Modules.Support.Domain;

namespace ModularShop.Modules.Support.Application.UseCases;

/// <summary>
/// Use case: open a new support ticket for a customer. It validates the customer against the SHARED
/// kernel <see cref="Customer"/> (read through the generic repository) and stamps the ticket with the
/// authenticated Identity user from the kernel's <see cref="ICurrentUser"/>.
/// </summary>
public sealed class CreateTicketUseCase : UseCase
{
    private readonly IReadRepository<Customer> _customers;
    private readonly IRepository<Ticket> _tickets;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CreateTicketUseCase> _logger;

    public CreateTicketUseCase(
        IReadRepository<Customer> customers,
        IRepository<Ticket> tickets,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<CreateTicketUseCase> logger)
    {
        _customers = customers;
        _tickets = tickets;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<TicketDto>> ExecuteAsync(CreateTicketRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Subject))
            return Result<TicketDto>.Invalid(new ValidationError("A ticket must have a subject."));

        var customer = await _customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, ct);
        if (customer is null)
            return Result<TicketDto>.NotFound($"Customer {request.CustomerId} was not found.");

        var ticket = new Ticket(
            request.Subject.Trim(),
            request.Description?.Trim() ?? string.Empty,
            customer.Id,
            customer.Name,
            _currentUser.UserId,
            _currentUser.UserName,
            DateTime.UtcNow);

        await _tickets.AddAsync(ticket, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Opened support ticket '{Subject}' for {Customer}.", ticket.Subject, customer.Name);

        return Result<TicketDto>.Success(ticket.ToDto());
    }
}
