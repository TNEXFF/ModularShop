using MediatR;
using ModularShop.Modules.Sales.Contracts;
using ModularShop.Modules.Shipping.Application;

namespace ModularShop.Modules.Shipping.Infrastructure.IntegrationEventHandlers;

/// <summary>
/// Reacts to the Sales module's <see cref="OrderPlaced"/> integration event (a MediatR
/// <c>INotification</c>) by opening a pending shipment. This is the other half of the asynchronous
/// flow (Warehouse decrements stock; Shipping creates the shipment) — both handlers subscribe to the
/// same event and run independently. The handler is a thin adapter over the
/// <see cref="CreateShipment"/> use case; MediatR discovers it when the host scans this assembly.
/// </summary>
internal sealed class CreateShipmentOnOrderPlaced : INotificationHandler<OrderPlaced>
{
    private readonly CreateShipment _createShipment;

    public CreateShipmentOnOrderPlaced(CreateShipment createShipment) => _createShipment = createShipment;

    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        var request = new NewShipment(
            notification.OrderId,
            notification.OrderNumber,
            notification.CustomerName,
            notification.Lines.Select(l => new NewShipmentItem(l.ProductName, l.Quantity)).ToList());
        return _createShipment.ExecuteAsync(request, cancellationToken);
    }
}
