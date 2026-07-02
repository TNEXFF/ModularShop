namespace ModularShop.SharedKernel.Messaging;

/// <summary>
/// Marker for an integration event — a fact one module publishes for others to react to.
/// Integration events are part of a module's PUBLIC contract, so they live in a module's
/// <c>.Contracts</c> project and are kept small and stable.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredOnUtc { get; }
}

/// <summary>Convenience base that stamps every event with an id and timestamp.</summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
