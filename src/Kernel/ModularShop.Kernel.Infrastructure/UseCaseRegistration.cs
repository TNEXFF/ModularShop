using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ModularShop.Kernel.Application;

namespace ModularShop.Kernel.Infrastructure;

/// <summary>
/// Convention-based use-case registration. Instead of listing every use case by hand, a module calls
/// <see cref="AddUseCases"/> for its Application assembly and every concrete <see cref="UseCase"/> in it
/// is registered as a scoped service (by its own concrete type — controllers depend on the concrete
/// use case).
/// </summary>
public static class UseCaseRegistration
{
    public static IServiceCollection AddUseCases(this IServiceCollection services, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
            if (type is { IsAbstract: false, IsClass: true } && typeof(UseCase).IsAssignableFrom(type))
                services.AddScoped(type);

        return services;
    }
}
