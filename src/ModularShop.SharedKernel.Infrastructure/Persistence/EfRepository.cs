using Microsoft.EntityFrameworkCore;
using ModularShop.SharedKernel.Domain;
using ModularShop.SharedKernel.Persistence;

namespace ModularShop.SharedKernel.Infrastructure.Persistence;

/// <summary>
/// Generic EF Core repository. It is parameterised by the module's DbContext type so that each
/// module wires its own context — e.g. <c>EfRepository&lt;Order, SalesDbContext&gt;</c> — keeping
/// the per-module data boundary intact. Reads default to <c>AsNoTracking</c> (a habit borrowed
/// from the Platform) since most reads don't need change tracking.
/// </summary>
public class EfRepository<TEntity, TContext> : IRepository<TEntity>
    where TEntity : Entity
    where TContext : DbContext
{
    protected readonly TContext Db;

    public EfRepository(TContext db) => Db = db;

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await Db.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken ct = default)
        => await Db.Set<TEntity>().AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(TEntity entity, CancellationToken ct = default)
        => await Db.Set<TEntity>().AddAsync(entity, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Db.SaveChangesAsync(ct);
}
