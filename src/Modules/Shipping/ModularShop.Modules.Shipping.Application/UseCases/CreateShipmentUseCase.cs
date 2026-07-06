using Microsoft.Extensions.Logging;
using ModularShop.Kernel.Application.Abstractions;
using ModularShop.Kernel.Domain.Repositories;
using ModularShop.Modules.Shipping.Domain;

namespace ModularShop.Modules.Shipping.Application.UseCases;

/// <summary>Input to <see cref="CreateShipmentUseCase"/> — the order data needed to open a shipment.</summary>
public sealed record NewShipment(
    Guid OrderId, string OrderNumber, Guid CustomerId, string CustomerName, IReadOnlyList<NewShipmentItem> Items);

public sealed record NewShipmentItem(string ProductName, int Quantity);

/// <summary>
/// Use case: open a new (Pending) shipment for a placed order. Invoked by the Shipping
/// integration-event handler when an order is placed — the other half of the asynchronous flow
/// (Warehouse decrements stock; Shipping creates the shipment).
/// </summary>
public sealed class CreateShipmentUseCase : UseCase
{
    private readonly IRepository<Shipment> _shipments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateShipmentUseCase> _logger;

    public CreateShipmentUseCase(IRepository<Shipment> shipments, IUnitOfWork unitOfWork, ILogger<CreateShipmentUseCase> logger)
    {
        _shipments = shipments;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteAsync(NewShipment request, CancellationToken ct)
    {
        var shipment = new Shipment(GenerateShipmentNumber(), request.OrderId,
            request.OrderNumber, request.CustomerId, request.CustomerName, DateTime.UtcNow);

        foreach (var item in request.Items)
            shipment.AddItem(item.ProductName, item.Quantity);

        await _shipments.AddAsync(shipment, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Shipping created shipment {ShipmentNumber} for order {OrderNumber}.",
            shipment.ShipmentNumber, request.OrderNumber);
    }

    private static string GenerateShipmentNumber() => $"SHP-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
}
