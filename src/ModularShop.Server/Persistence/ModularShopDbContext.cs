using Microsoft.EntityFrameworkCore;
using ModularShop.Kernel.Infrastructure.Persistence;

namespace ModularShop.Server.Persistence;

/// <summary>
/// The single runtime DbContext for the whole application — the "host context". It derives from the
/// kernel's Identity + shared-entities base (<see cref="KernelDbContext"/>), then layers every module's
/// model on top by asking each registered <see cref="IModuleModel"/> to contribute. This one context
/// owns the database and the (centralised) migrations; modules only supply blueprints. Every service in
/// the app resolves the base <c>DbContext</c>, which is aliased to this type in <c>Program.cs</c>.
/// </summary>
public sealed class ModularShopDbContext : KernelDbContext
{
    /// <summary>Schema for everything the kernel owns (shared entities + all Identity tables).</summary>
    public const string KernelSchema = "kernel";

    private readonly IReadOnlyList<IModuleModel> _modules;

    public ModularShopDbContext(DbContextOptions<ModularShopDbContext> options, IEnumerable<IModuleModel> modules)
        : base(options)
        => _modules = modules.ToList();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Identity + the shared kernel entities (Customer, Currency)

        // Each module contributes its entities (reflected from its blueprint DbSets) + special config.
        foreach (var module in _modules)
            modelBuilder.ApplyModuleModel(module);

        // Finally, place every table in its owner's schema (modules → their schema; the rest → kernel).
        modelBuilder.ApplyModuleSchemas(_modules, KernelSchema);
    }
}
