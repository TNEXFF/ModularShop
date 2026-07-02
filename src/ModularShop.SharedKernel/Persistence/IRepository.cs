using ModularShop.SharedKernel.Domain;

namespace ModularShop.SharedKernel.Persistence;

/// <summary>
/// A deliberately thin generic repository (the pattern adopted, simplified, from the Platform's
/// IRepository&lt;T&gt;). Modules may use it for common add/get/list/save, or query their own
/// DbContext directly for anything richer. Reads default to no-tracking in the implementation.
/// </summary>
public interface IRepository<TEntity> where TEntity : Entity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
