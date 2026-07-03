using Microsoft.AspNetCore.Mvc;
using ModularShop.Kernel.Web;
using ModularShop.Modules.Support.Application;

namespace ModularShop.Modules.Support.Api.Controllers;

/// <summary>
/// Support-ticket endpoints. Each action invokes a single use case and returns the uniform
/// <see cref="ApiResponse{T}"/> envelope. Like every module controller it requires an authenticated user
/// (via <see cref="ApiControllerBase"/>).
/// </summary>
[Route("api/tickets")]
public sealed class TicketsController : ApiControllerBase
{
    private readonly ListTickets _listTickets;
    private readonly GetTicket _getTicket;
    private readonly CreateTicket _createTicket;
    private readonly AddTicketMessage _addTicketMessage;
    private readonly ChangeTicketStatus _changeTicketStatus;

    public TicketsController(
        ListTickets listTickets,
        GetTicket getTicket,
        CreateTicket createTicket,
        AddTicketMessage addTicketMessage,
        ChangeTicketStatus changeTicketStatus)
    {
        _listTickets = listTickets;
        _getTicket = getTicket;
        _createTicket = createTicket;
        _addTicketMessage = addTicketMessage;
        _changeTicketStatus = changeTicketStatus;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TicketListItemDto>>>> List(CancellationToken ct)
        => ToApiResponse(await _listTickets.ExecuteAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TicketDto>>> Get(Guid id, CancellationToken ct)
        => ToApiResponse(await _getTicket.ExecuteAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TicketDto>>> Create([FromBody] CreateTicketRequest request, CancellationToken ct)
        => ToApiResponse(await _createTicket.ExecuteAsync(request, ct));

    [HttpPost("{id:guid}/messages")]
    public async Task<ActionResult<ApiResponse<TicketDto>>> AddMessage(Guid id, [FromBody] AddMessageRequest request, CancellationToken ct)
        => ToApiResponse(await _addTicketMessage.ExecuteAsync(id, request, ct));

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<TicketDto>>> ChangeStatus(Guid id, [FromBody] ChangeStatusRequest request, CancellationToken ct)
        => ToApiResponse(await _changeTicketStatus.ExecuteAsync(id, request, ct));
}
