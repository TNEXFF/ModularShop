using Microsoft.EntityFrameworkCore;
using ModularShop.Kernel.Domain;
using ModularShop.Kernel.Domain.Repositories;

namespace ModularShop.Kernel.Infrastructure.Persistence.Repositories;

/// <summary>
/// The generic read/write repository — adds the write operations to <see cref="ReadRepository{T}"/>.
/// Writes only <b>stage</b> changes on the context; the <c>UnitOfWork</c> commits them. Registered
/// open-generically for both <see cref="IReadRepository{T}"/> and <see cref="IRepository{T}"/>.
/// </summary>
public class Repository<T> : ReadRepository<T>, IRepository<T> where T : Entity
{
    public Repository(DbContext context) : base(context) { }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await Set.AddAsync(entity, cancellationToken);

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        => await Set.AddRangeAsync(entities, cancellationToken);

    public virtual void Update(T entity) => Set.Update(entity);

    public virtual void Remove(T entity) => Set.Remove(entity);

    public virtual void RemoveRange(IEnumerable<T> entities) => Set.RemoveRange(entities);
}
