using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ModularShop.Kernel.Infrastructure;

/// <summary>
/// The one place module composition happens. Instead of a hand-maintained list, this scans the app's own
/// assemblies for <see cref="IModule"/> implementations, selects which to activate from configuration,
/// and lets each register its own parts (services, use cases, controllers, event bus, seeders). At startup
/// the host also calls <see cref="InitializeModulesAsync"/> to migrate the one database and run each
/// module's seeder in order. A new micro-solution reuses this verbatim: it references the module projects
/// it wants and, optionally, names them under "Modules" in appsettings.json.
/// </summary>
public static class ModuleRegistration
{
    /// <summary>
    /// Discovers every <see cref="IModule"/> in the app's assemblies, keeps those selected by the
    /// "Modules" configuration array (foundational modules are always kept; if the key is absent, ALL are
    /// kept), then registers each and lets it register its own services.
    /// </summary>
    public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration configuration)
    {
        // SelectModules already returns the chosen set, foundational-first — so no re-ordering here.
        var modules = SelectModules(DiscoverModules(), configuration);
        ValidateRequiredModules(modules);

        foreach (var module in modules)
        {
            services.AddSingleton<IModule>(module);
            module.Register(services, configuration);
        }

        return services;
    }

    /// <summary>
    /// Applies the centralised migrations ONCE, then runs every registered module's seeder in <c>Order</c>.
    /// The host calls this at startup. It resolves the base <see cref="DbContext"/> (aliased to the single
    /// host context in Program.cs), so it never needs to know the host's concrete context type.
    /// </summary>
    public static async Task InitializeModulesAsync(
        this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();

        // Centralised migrations: bring the one database up to date once, before any module seeds.
        var db = scope.ServiceProvider.GetRequiredService<DbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        // Then let each module (the kernel first, via Order) seed its own tables through the shared context.
        foreach (var initializer in scope.ServiceProvider.GetServices<IModuleInitializer>().OrderBy(i => i.Order))
            await initializer.InitializeAsync(cancellationToken);
    }

    // Scan the DEPLOYED assemblies next to the host, not AppDomain.CurrentDomain.GetAssemblies(): referenced
    // assemblies are loaded lazily, so a not-yet-touched module would be missing from AppDomain at startup.
    // The set of ModularShop.*.dll in the output folder IS exactly "the modules this host references".
    private static IReadOnlyList<IModule> DiscoverModules()
    {
        var modules = new List<IModule>();

        foreach (var dll in Directory.EnumerateFiles(AppContext.BaseDirectory, "ModularShop.*.dll"))
        {
            var assembly = Assembly.Load(AssemblyName.GetAssemblyName(dll));

            foreach (var type in assembly.GetTypes())
                if (type is { IsClass: true, IsAbstract: false } && typeof(IModule).IsAssignableFrom(type))
                    modules.Add((IModule)Activator.CreateInstance(type)!);
        }

        return modules;
    }

    // Keep foundational modules always; keep the rest only if named in "Modules" (absent ⇒ keep all).
    // Foundational-first ordering means the kernel composes before the features that FK to its entities.
    private static IReadOnlyList<IModule> SelectModules(IReadOnlyList<IModule> discovered, IConfiguration configuration)
    {
        var enabledNames = configuration.GetSection("Modules").Get<string[]>();

        var selected = enabledNames is null
            ? discovered
            : discovered.Where(m => m.IsFoundational
                                 || enabledNames.Contains(m.Name, StringComparer.OrdinalIgnoreCase));

        return selected.OrderByDescending(m => m.IsFoundational).ToList();
    }

    // Fail fast when the selected set is incomplete. A module declares its cross-module runtime needs via
    // IModule.RequiredModules (e.g. Sales needs Warehouse for its synchronous IWarehouseApi call). Without
    // this, a bad "Modules" selection would only surface later as a DI resolution error on the first request
    // that reaches the missing module. This runs for EVERY host — the demo and any packaged micro-solution.
    private static void ValidateRequiredModules(IReadOnlyList<IModule> selected)
    {
        var selectedNames = selected.Select(m => m.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var errors = selected
            .SelectMany(module => module.RequiredModules
                .Where(required => !selectedNames.Contains(required))
                .Select(required => $"Module '{module.Name}' requires module '{required}', but '{required}' is not enabled."))
            .ToArray();

        if (errors.Length > 0)
            throw new InvalidOperationException(
                "Invalid module selection:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
    }
}
