namespace ModularShop.SharedKernel.Messaging;

/// <summary>
/// Publishes integration events to in-process handlers. This is the ASYNCHRONOUS inter-module
/// communication channel: the publisher announces that something happened and does not know
/// (or care) which modules react. Contrast with a synchronous call through a module's public
/// interface, used when the caller needs an answer immediately.
/// </summary>
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
