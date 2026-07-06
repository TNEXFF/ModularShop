using Microsoft.EntityFrameworkCore;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Kernel.Infrastructure.Persistence;

namespace ModularShop.Server.Persistence;

/// <summary>
/// The single runtime DbContext for the whole application — the "host context". It owns the database and
/// the (centralised) migrations, but holds NO entities of its own: it composes the model by asking every
/// registered module — <b>the kernel included</b> — to layer its own context's model onto this one (see
/// <see cref="IModelContributor"/> / <see cref="ModuleDbContext"/>). Every service in the app resolves the
/// base <c>DbContext</c>, which is aliased to this type in <c>Program.cs</c>, so one open-generic
/// repository and the Identity stores all run against this one context.
/// </summary>
public sealed class ModularShopDbContext : DbContext
{
    private readonly IReadOnlyList<IModule> _modules;

    public ModularShopDbContext(DbContextOptions<ModularShopDbContext> options, IEnumerable<IModule> modules)
        : base(options)
        => _modules = modules.ToList();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Each module contributes its slice of the model. The kernel is first in the list (HostModules):
        // it sets the default "kernel" schema and owns Identity + the shared entities modules FK to, so
        // those principals exist before a module adds a cross-schema foreign key to them.
        foreach (var module in _modules)
        {
            var contributor = CreateContributor(module.ContextType);
            contributor.ApplyModel(modelBuilder);
            (contributor as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// Instantiates a module's context purely to harvest its model. The context is never connected to a
    /// database, but it is given a throwaway SqlServer options object (never opened) so that Identity's
    /// <c>OnModelCreating</c>, which reads store options, has a provider to look at.
    /// </summary>
    private static IModelContributor CreateContributor(Type contextType)
    {
        var builderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType);
        var builder = (DbContextOptionsBuilder)Activator.CreateInstance(builderType)!;
        builder.UseSqlServer("Server=_;Database=_;");
        return (IModelContributor)Activator.CreateInstance(contextType, builder.Options)!;
    }
}
