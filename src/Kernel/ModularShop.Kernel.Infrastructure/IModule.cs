using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ModularShop.Kernel.Infrastructure;

/// <summary>
/// A module's self-registration contract. Each module — <b>including the kernel</b>, which is just a
/// special module — implements this to register ALL of its own parts (services, use cases, controllers,
/// event bus, seeders) and to declare the <see cref="ContextType"/> it contributes to the single host
/// model. The host (composition root) holds the list of modules, calls <see cref="Register"/> on each,
/// and composes their <see cref="ContextType"/>s into one runtime context — it contains no module logic
/// of its own.
/// </summary>
public interface IModule
{
    /// <summary>Human-readable module name (used in logs / diagnostics).</summary>
    string Name { get; }

    /// <summary>
    /// The module's DbContext type. It declares the module's entities and configures them (and their
    /// schema) in its own <c>OnModelCreating</c>; the host instantiates it purely to harvest that model
    /// (see <c>IModelContributor</c> / <c>ModuleDbContext</c>).
    /// </summary>
    Type ContextType { get; }

    /// <summary>Register everything the module owns: its services, use cases, controllers, event handlers and initializer.</summary>
    void Register(IServiceCollection services, IConfiguration configuration);
}
