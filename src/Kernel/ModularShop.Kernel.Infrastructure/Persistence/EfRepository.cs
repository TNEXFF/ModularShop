using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ModularShop.Kernel.Infrastructure.Persistence;

/// <summary>
/// Generic EF Core repository built on Ardalis.Specification's <see cref="RepositoryBase{T}"/>
/// (this replaces the previous hand-rolled repository). It is parameterised by the module's
/// DbContext type so each module binds its own context — e.g. <c>EfRepository&lt;Order, SalesDbContext&gt;</c>
/// — keeping the per-module data boundary intact. Queries are expressed as <c>Specification</c>
/// objects in the module's Application layer, so the repository stays generic and the Application
/// layer never references the concrete DbContext.
/// </summary>
public sealed class EfRepository<TEntity, TContext> : RepositoryBase<TEntity>
    where TEntity : class
    where TContext : DbContext
{
    public EfRepository(TContext dbContext) : base(dbContext) { }
}
