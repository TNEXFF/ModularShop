using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ModularShop.Kernel.Domain;
using ModularShop.Kernel.Domain.Repositories;

namespace ModularShop.Kernel.Infrastructure.Persistence.Repositories;

/// <summary>
/// The generic read repository over the single host <see cref="DbContext"/>. Because Option B composes
/// every module's entities into one context, a single open-generic implementation serves them all —
/// <c>Order</c>, <c>Product</c>, <c>Shipment</c>, <c>Ticket</c>, the shared <c>Customer</c>, and so on.
/// <para>
/// It is <b>public</b> (not internal) so a module can subclass it in its own Infrastructure assembly to
/// add a specific repository (see the Support module's <c>TicketRepository</c>). The one query-building
/// hook, <see cref="Query"/>, is <c>protected</c> so those subclasses can compose freely while the
/// Application layer only ever sees the materialised <see cref="IReadRepository{T}"/> methods.
/// </para>
/// </summary>
public class ReadRepository<T> : IReadRepository<T> where T : Entity
{
    protected readonly DbContext Context;
    protected readonly DbSet<T> Set;

    public ReadRepository(DbContext context)
    {
        Context = context;
        Set = context.Set<T>();
    }

    /// <summary>
    /// The single place queries are shaped: tracking, typed includes, filtering, ordering. Reads pass
    /// <c>tracking: false</c>; the tracked overloads pass <c>true</c>. Kept <c>protected</c> for the
    /// repository and its module subclasses; never exposed to the Application layer.
    /// </summary>
    protected IQueryable<T> Query(
        bool tracking,
        Expression<Func<T, bool>>? predicate = null,
        IEnumerable<Expression<Func<T, object?>>>? typedIncludes = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null)
    {
        IQueryable<T> query = tracking ? Set : Set.AsNoTracking();

        if (typedIncludes is not null)
            foreach (var include in typedIncludes)
                query = query.Include(include);

        if (predicate is not null)
            query = query.Where(predicate);

        if (orderBy is not null)
            query = orderBy(query);

        return query;
    }

    // ── By-key gets: TRACKED (for load-then-modify flows) ───────────────────────────────────────────
    public virtual async Task<IReadOnlyList<T>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return Array.Empty<T>();

        return await Set.Where(e => ids.Contains(e.Id)).ToListAsync(cancellationToken);
    }

    public virtual async Task<T?> GetForUpdateAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default,
        params Expression<Func<T, object?>>[] includes)
        => await Query(tracking: true, predicate: predicate, typedIncludes: includes).FirstOrDefaultAsync(cancellationToken);

    // ── Predicate reads: NoTracking ─────────────────────────────────────────────────────────────────
    public virtual async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await Query(tracking: false, predicate: predicate).FirstOrDefaultAsync(cancellationToken);

    public virtual async Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await Set.AnyAsync(predicate, cancellationToken);

    public virtual async Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => predicate is null
            ? await Set.CountAsync(cancellationToken)
            : await Set.CountAsync(predicate, cancellationToken);

    public virtual async Task<IReadOnlyList<T>> ListAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        CancellationToken cancellationToken = default)
        => await Query(tracking: false, predicate: predicate, orderBy: orderBy).ToListAsync(cancellationToken);

    public virtual async Task<T?> GetWithIncludesAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default,
        params Expression<Func<T, object?>>[] includes)
        => await Query(tracking: false, predicate: predicate, typedIncludes: includes).FirstOrDefaultAsync(cancellationToken);

    public virtual async Task<IReadOnlyList<T>> ListWithIncludesAsync(
        Expression<Func<T, bool>>? predicate,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy,
        CancellationToken cancellationToken = default,
        params Expression<Func<T, object?>>[] includes)
        => await Query(tracking: false, predicate: predicate, typedIncludes: includes, orderBy: orderBy).ToListAsync(cancellationToken);
}
