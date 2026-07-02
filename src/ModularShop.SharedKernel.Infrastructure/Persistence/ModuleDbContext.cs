using Microsoft.EntityFrameworkCore;

namespace ModularShop.SharedKernel.Infrastructure.Persistence;

/// <summary>
/// Base DbContext for a module. It enforces <b>schema-per-module</b>: every table this context
/// owns is placed in the module's own MSSQL schema, and the module's entity configurations are
/// auto-applied from its assembly. Each module derives exactly one of these — modules never
/// share a DbContext, which is the single most important rule for data isolation in an MM.
/// </summary>
public abstract class ModuleDbContext : DbContext
{
    protected ModuleDbContext(DbContextOptions options) : base(options) { }

    /// <summary>The MSSQL schema this module owns (e.g. "sales", "warehouse", "shipping").</summary>
    protected abstract string Schema { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
