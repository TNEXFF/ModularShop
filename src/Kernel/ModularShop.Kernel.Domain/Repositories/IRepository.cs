namespace ModularShop.Kernel.Domain.Repositories;

/// <summary>
/// Full read/write access to an entity type. Adds the write operations to <see cref="IReadRepository{T}"/>.
/// <para>
/// The repository only <b>stages</b> changes on the context; it does not persist them. Committing is the
/// separate responsibility of the <c>IUnitOfWork</c>, so a use case decides exactly when (and how often)
/// it saves — one transaction per unit of work.
/// </para>
/// </summary>
public interface IRepository<T> : IReadRepository<T> where T : Entity
{
    /// <summary>Stages a new entity for insertion.</summary>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>Stages an entity as modified. (Entities loaded tracked are already observed — this is for detached ones.)</summary>
    void Update(T entity);

    /// <summary>Stages an entity for deletion.</summary>
    void Remove(T entity);
}
