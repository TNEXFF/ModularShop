using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Modules.Shipping.Application.UseCases;
using ModularShop.Modules.Shipping.Infrastructure.Persistence;

namespace ModularShop.Modules.Shipping.Infrastructure;

/// <summary>
/// The Shipping module's composition root (its <see cref="IModule"/>). It exposes NO <c>.Contracts</c>
/// project — nothing else calls into Shipping. It consumes the Sales <c>OrderPlaced</c> event (via its
/// integration-event handler, so it registers MediatR over its own assembly), registers its use cases and
/// seeder, and declares the <see cref="ShippingDbContext"/>.
/// </summary>
public sealed class ShippingModule : IModule
{
    public string Name => "Shipping";
    public Type ContextType => typeof(ShippingDbContext);

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddUseCases(typeof(CreateShipmentUseCase).Assembly);

        // Shipping subscribes to OrderPlaced — its INotificationHandler lives in this assembly.
        services.AddMediatR(cfg =>
        {
            var licenseKey = configuration["MediatR:LicenseKey"];
            if (!string.IsNullOrWhiteSpace(licenseKey))
                cfg.LicenseKey = licenseKey;
            cfg.RegisterServicesFromAssembly(typeof(ShippingModule).Assembly);
        });

        services.AddScoped<IModuleInitializer, ShippingSeeder>();
    }
}
