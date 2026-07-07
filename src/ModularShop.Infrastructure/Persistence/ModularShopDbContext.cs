using Microsoft.EntityFrameworkCore;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Kernel.Infrastructure.Persistence;

namespace ModularShop.Infrastructure.Persistence;

/// <summary>
/// The single runtime DbContext for the whole application — the "host context". It owns the database and
/// the (centralised) migrations, but holds NO entities of its own: it composes the model by asking every
/// registered module — <b>the kernel included</b> — to layer its own context's model onto this one (see
/// <see cref="ModuleModelComposition.ApplyModuleModels"/>). Every service in the app resolves the base
/// <c>DbContext</c>, aliased to this type in <c>Program.cs</c>, so one open-generic repository and the
/// Identity stores all run against this one context. It lives in the host's Infrastructure layer alongside
/// the migrations it owns.
/// </summary>
public sealed class ModularShopDbContext : DbContext
{
    private readonly IReadOnlyList<IModule> _modules;

    public ModularShopDbContext(DbContextOptions<ModularShopDbContext> options, IEnumerable<IModule> modules)
        : base(options)
        => _modules = modules.ToList();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => this.ApplyModuleModels(modelBuilder, _modules);
}
