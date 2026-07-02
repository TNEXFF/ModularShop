using Microsoft.Extensions.DependencyInjection;
using ModularShop.SharedKernel.Infrastructure.Messaging;
using ModularShop.SharedKernel.Messaging;

namespace ModularShop.SharedKernel.Infrastructure;

public static class SharedInfrastructureExtensions
{
    /// <summary>Registers cross-cutting infrastructure shared by all modules (the event bus).</summary>
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
    {
        // Scoped so handlers resolve within the same request scope (and thus the same DbContexts).
        services.AddScoped<IEventBus, InMemoryEventBus>();
        return services;
    }
}
