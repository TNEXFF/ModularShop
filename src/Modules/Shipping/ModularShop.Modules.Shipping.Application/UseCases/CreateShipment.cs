using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using ModularShop.Modules.Shipping.Domain;

namespace ModularShop.Modules.Shipping.Application;

/// <summary>Input to <see cref="CreateShipment"/> — the order data needed to open a shipment.</summary>
public sealed record NewShipment(Guid OrderId, string OrderNumber, string CustomerName, IReadOnlyList<NewShipmentItem> Items);

public sealed record NewShipmentItem(string ProductName, int Quantity);

/// <summary>
/// Use case: open a new (Pending) shipment for a placed order. Invoked by the Shipping
/// integration-event handler when an order is placed. This is the other half of the asynchronous
/// flow (Warehouse decrements stock; Shipping creates the shipment).
/// </summary>
public sealed class CreateShipment
{
    private readonly IRepositoryBase<Shipment> _shipments;
    private readonly ILogger<CreateShipment> _logger;

    public CreateShipment(IRepositoryBase<Shipment> shipments, ILogger<CreateShipment> logger)
    {
        _shipments = shipments;
        _logger = logger;
    }

    public async Task ExecuteAsync(NewShipment request, CancellationToken ct)
    {
        var shipment = new Shipment(
            Guid.NewGuid(), GenerateShipmentNumber(), request.OrderId, request.OrderNumber, request.CustomerName, DateTime.UtcNow);

        foreach (var item in request.Items)
            shipment.AddItem(item.ProductName, item.Quantity);

        await _shipments.AddAsync(shipment, ct);
        await _shipments.SaveChangesAsync(ct);
        _logger.LogInformation("Shipping created shipment {ShipmentNumber} for order {OrderNumber}.",
            shipment.ShipmentNumber, request.OrderNumber);
    }

    private static string GenerateShipmentNumber() => $"SHP-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
}
