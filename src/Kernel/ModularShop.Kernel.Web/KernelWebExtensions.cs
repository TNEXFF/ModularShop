using Microsoft.Extensions.DependencyInjection;
using ModularShop.Kernel.Application;

namespace ModularShop.Kernel.Web;

public static class KernelWebExtensions
{
    /// <summary>Registers kernel web cross-cutting services (the current-user accessor).</summary>
    public static IServiceCollection AddKernelWeb(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        return services;
    }
}
