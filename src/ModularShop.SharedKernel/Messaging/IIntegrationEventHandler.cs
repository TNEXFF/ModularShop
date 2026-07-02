namespace ModularShop.SharedKernel.Messaging;

/// <summary>
/// Handles an integration event published by another module. A module registers one of these
/// per event it cares about; the event bus resolves and invokes them all.
/// </summary>
public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
