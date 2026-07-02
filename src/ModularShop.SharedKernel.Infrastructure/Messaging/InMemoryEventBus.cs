using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModularShop.SharedKernel.Messaging;

namespace ModularShop.SharedKernel.Infrastructure.Messaging;

/// <summary>
/// The simplest correct integration-event bus: it resolves every registered
/// <see cref="IIntegrationEventHandler{TEvent}"/> from the current DI scope and invokes them
/// in-process. Because it is ~30 lines you can read the whole mechanism — nothing is hidden
/// behind a library.
/// <para>
/// A production system would replace this with a transactional <i>outbox</i> plus a message
/// broker so events survive a process crash, but the module-facing contract
/// (<see cref="IEventBus"/>) would not change — that is the point of programming to the interface.
/// </para>
/// </summary>
public sealed class InMemoryEventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InMemoryEventBus> _logger;

    public InMemoryEventBus(IServiceProvider serviceProvider, ILogger<InMemoryEventBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        var handlers = _serviceProvider.GetServices<IIntegrationEventHandler<TEvent>>();
        foreach (var handler in handlers)
        {
            _logger.LogInformation("Integration event {Event} -> handler {Handler}",
                typeof(TEvent).Name, handler.GetType().Name);
            await handler.HandleAsync(@event, cancellationToken);
        }
    }
}
