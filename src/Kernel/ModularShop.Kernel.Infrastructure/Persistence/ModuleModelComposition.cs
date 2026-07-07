using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace ModularShop.Kernel.Infrastructure.Persistence;

/// <summary>
/// Composes the single host model out of the registered modules. Each module owns an ordinary
/// <see cref="DbContext"/> that configures its entities (and their schema) in its own protected
/// <c>OnModelCreating</c>; this invokes that method, by reflection, onto the one shared
/// <see cref="ModelBuilder"/>. That is what lets a module context be "just a DbContext" — no base class
/// or interface to surface the protected method.
/// </summary>
public static class ModuleModelComposition
{
    public static void ApplyModuleModels(
        this DbContext hostContext, ModelBuilder modelBuilder, IEnumerable<IModule> modules)
    {
        // Foundational first: the kernel sets the default schema and defines the entities (Customer,
        // Currency, Identity users) that feature modules create cross-schema foreign keys to.
        foreach (var module in modules.OrderByDescending(m => m.IsFoundational))
        {
            var moduleContext = CreateForModelHarvest(module.ContextType);
            OnModelCreatingOf(module.ContextType).Invoke(moduleContext, [modelBuilder]);
            (moduleContext as IDisposable)?.Dispose();
        }
    }

    // The module context is instantiated purely to harvest its model — never connected. It is still given a
    // throwaway SqlServer options object (never opened) because Identity's OnModelCreating reads store options.
    private static DbContext CreateForModelHarvest(Type contextType)
    {
        var builderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType);
        var builder = (DbContextOptionsBuilder)Activator.CreateInstance(builderType)!;
        builder.UseSqlServer("Server=_;Database=_;");
        return (DbContext)Activator.CreateInstance(contextType, builder.Options)!;
    }

    private static MethodInfo OnModelCreatingOf(Type contextType) =>
        contextType.GetMethod(
            "OnModelCreating", BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null, types: [typeof(ModelBuilder)], modifiers: null)
        ?? throw new InvalidOperationException(
            $"Module context {contextType.Name} defines no protected OnModelCreating(ModelBuilder).");
}
