using MediatR;
using ModularShop.Modules.Sales.Contracts;
using ModularShop.Modules.Warehouse.Application.UseCases;

namespace ModularShop.Modules.Warehouse.Infrastructure.IntegrationEventHandlers;

/// <summary>
/// Reacts to the Sales module's <see cref="OrderPlaced"/> integration event (a MediatR
/// <c>INotification</c>) by decrementing stock for each ordered product. The handler is a thin
/// adapter: it translates the event into the Warehouse's <see cref="DecrementStock"/> use case.
/// MediatR discovers this handler when the host scans the Warehouse Infrastructure assembly.
/// </summary>
internal sealed class DecrementStockOnOrderPlaced : INotificationHandler<OrderPlaced>
{
    private readonly DecrementStock _decrementStock;

    public DecrementStockOnOrderPlaced(DecrementStock decrementStock) => _decrementStock = decrementStock;

    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        var changes = notification.Lines
            .Select(l => new ProductStockChange(l.ProductId, l.Quantity))
            .ToList();
        return _decrementStock.ExecuteAsync(changes, cancellationToken);
    }
}
