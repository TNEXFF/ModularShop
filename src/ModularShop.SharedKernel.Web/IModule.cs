using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ModularShop.SharedKernel.Web;

/// <summary>
/// A module's self-registration contract — a deliberately small version of the Platform's rich
/// <c>IModule</c>. Each module implements this to (1) register its own services and DbContext and
/// (2) map its own HTTP endpoints. The API host (composition root) discovers modules and calls
/// these methods; the host itself contains no business logic. Adding a module = implementing this
/// interface and listing the module once in the host.
/// </summary>
public interface IModule
{
    /// <summary>Human-readable module name (used in logs / diagnostics).</summary>
    string Name { get; }

    /// <summary>Register the module's own services, DbContext, event handlers and initializer.</summary>
    void Register(IServiceCollection services, IConfiguration configuration);

    /// <summary>Map the module's HTTP endpoints (minimal APIs).</summary>
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
