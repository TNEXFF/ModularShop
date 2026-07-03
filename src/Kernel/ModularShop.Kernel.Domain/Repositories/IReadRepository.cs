using System.Linq.Expressions;

namespace ModularShop.Kernel.Domain.Repositories;

/// <summary>
/// Read access to an entity type, expressed as <b>materialised, asynchronous</b> methods. Because every
/// method returns a finished result (an entity, a list, a bool, a count) — never an <see cref="IQueryable{T}"/>
/// — the Application layer can depend on this interface <b>without referencing EF Core</b>. The query is
/// built and executed inside the Infrastructure implementation.
/// <para>
/// Tracking convention (matching the reference solutions): reads through a predicate are
/// <c>AsNoTracking</c>; the by-key gets (<see cref="GetByIdAsync"/>, <see cref="GetByIdsAsync"/>) and
/// <see cref="GetForUpdateAsync"/> return <b>tracked</b> entities, ready to be mutated and committed via
/// the <c>IUnitOfWork</c>.
/// </para>
/// </summary>
/// <typeparam name="T">An entity type (has an <see cref="Entity.Id"/>).</typeparam>
public interface IReadRepository<T> where T : Entity
{
    /// <summary>Loads a single entity by its key. <b>Tracked</b> — use for load-then-modify flows.</summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Loads the entities with the given keys. <b>Tracked</b>.</summary>
    Task<IReadOnlyList<T>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single entity matching <paramref name="predicate"/>, <b>tracked</b> and eagerly loading the
    /// given navigations — for a load-then-modify flow that also needs related data (e.g. a shipment with
    /// its items). Prefer the NoTracking reads below for pure queries.
    /// </summary>
    Task<T?> GetForUpdateAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default,
        params Expression<Func<T, object?>>[] includes);

    /// <summary>First entity matching <paramref name="predicate"/>, or null. NoTracking.</summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Whether any entity matches <paramref name="predicate"/>.</summary>
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Count of entities (optionally matching <paramref name="predicate"/>).</summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists entities, optionally filtered and ordered. NoTracking. Ordering is expressed as a delegate
    /// over <see cref="IQueryable{T}"/> (<c>System.Linq</c>, not EF Core) so it still runs in the database
    /// without pulling EF Core into the Application layer, e.g. <c>orderBy: q =&gt; q.OrderBy(x =&gt; x.Name)</c>.
    /// </summary>
    Task<IReadOnlyList<T>> ListAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Single entity matching <paramref name="predicate"/>, NoTracking, eagerly loading the given
    /// navigations. Includes are <b>typed</b> (compile-time safe), e.g. <c>o =&gt; o.Lines</c>.
    /// </summary>
    Task<T?> GetWithIncludesAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default,
        params Expression<Func<T, object?>>[] includes);

    /// <summary>List (NoTracking) with optional filter/order and <b>typed</b> eager-loaded navigations.</summary>
    Task<IReadOnlyList<T>> ListWithIncludesAsync(
        Expression<Func<T, bool>>? predicate,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy,
        CancellationToken cancellationToken = default,
        params Expression<Func<T, object?>>[] includes);

    /// <summary>
    /// List (NoTracking) eagerly loading navigations named by <b>string path</b> — for cross-cutting
    /// navigations that cannot be referenced by type across an assembly boundary, e.g. <c>"Customer.Address"</c>.
    /// </summary>
    Task<IReadOnlyList<T>> ListWithIncludesAsync(
        Expression<Func<T, bool>>? predicate,
        IReadOnlyCollection<string> stringIncludes,
        CancellationToken cancellationToken = default);
}
