namespace ModularShop.Kernel.Domain;

/// <summary>
/// Base class for every persistent entity. An entity has a stable identity (<see cref="Id"/>).
/// The kernel's Domain layer deliberately keeps this tiny — it holds primitives, never business rules.
/// <para>
/// The <see cref="Id"/> is <b>not</b> assigned in code: it is left as the default (empty) Guid so EF Core
/// generates a (sequential) value when the row is inserted. Seed data may still pass an explicit id
/// through the aggregate constructors — EF honours an explicitly-set key and generates one only when the
/// value is left empty.
/// </para>
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; }

    protected Entity() { }

    /// <summary>Used by seeding to pin a deterministic id; runtime callers pass <c>default</c> so EF generates one.</summary>
    protected Entity(Guid id) => Id = id;
}
