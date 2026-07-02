using Microsoft.Extensions.Logging;
using ModularShop.Modules.Sales.Contracts;
using ModularShop.Modules.Shipping.Domain;
using ModularShop.Modules.Shipping.Infrastructure;
using ModularShop.SharedKernel.Messaging;

namespace ModularShop.Modules.Shipping.Application.IntegrationEventHandlers;

/// <summary>
/// Reacts to the Sales module's <see cref="OrderPlaced"/> event by creating a pending shipment.
/// This is the other half of the asynchronous flow (Warehouse decrements stock; Shipping creates
/// the shipment). Both handlers subscribe to the same event and run independently.
/// </summary>
internal sealed class CreateShipmentOnOrderPlaced : IIntegrationEventHandler<OrderPlaced>
{
    private readonly ShippingDbContext _db;
    private readonly ILogger<CreateShipmentOnOrderPlaced> _logger;

    public CreateShipmentOnOrderPlaced(ShippingDbContext db, ILogger<CreateShipmentOnOrderPlaced> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(OrderPlaced @event, CancellationToken cancellationToken = default)
    {
        var shipment = new Shipment(
            Guid.NewGuid(), GenerateShipmentNumber(), @event.OrderId, @event.OrderNumber, @event.CustomerName, DateTime.UtcNow);

        foreach (var line in @event.Lines)
            shipment.AddItem(line.ProductName, line.Quantity);

        _db.Shipments.Add(shipment);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Shipping created shipment {ShipmentNumber} for order {OrderNumber}.",
            shipment.ShipmentNumber, @event.OrderNumber);
    }

    private static string GenerateShipmentNumber() => $"SHP-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
}
