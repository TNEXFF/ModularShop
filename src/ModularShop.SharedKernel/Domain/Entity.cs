namespace ModularShop.SharedKernel.Domain;

/// <summary>
/// Base class for every persistent entity. An entity has a stable identity (<see cref="Id"/>).
/// The shared kernel deliberately keeps this tiny — it holds primitives, never business rules.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    protected Entity() { }

    protected Entity(Guid id) => Id = id;
}
