using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ModularShop.Kernel.Infrastructure;

/// <summary>
/// A module's self-registration contract, implemented in each module's Infrastructure layer. It
/// lets a module register its own services, DbContext, use cases, event handlers and initializer.
/// The host (composition root) holds the list of modules and calls <see cref="Register"/> on each;
/// the host itself contains no business logic.
/// <para>
/// Note there is no <c>MapEndpoints</c>: HTTP endpoints are now MVC controllers living in each
/// module's Api project, which the host registers as ASP.NET Core <c>ApplicationPart</c>s.
/// </para>
/// </summary>
public interface IModule
{
    /// <summary>Human-readable module name (used in logs / diagnostics).</summary>
    string Name { get; }

    /// <summary>Register the module's own services, DbContext, use cases, handlers and initializer.</summary>
    void Register(IServiceCollection services, IConfiguration configuration);
}
