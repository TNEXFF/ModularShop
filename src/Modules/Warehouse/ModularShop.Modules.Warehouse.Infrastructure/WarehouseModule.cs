using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Modules.Warehouse.Api.Controllers;
using ModularShop.Modules.Warehouse.Application;
using ModularShop.Modules.Warehouse.Application.UseCases;
using ModularShop.Modules.Warehouse.Contracts;
using ModularShop.Modules.Warehouse.Infrastructure.Persistence;

namespace ModularShop.Modules.Warehouse.Infrastructure;

/// <summary>
/// The Warehouse module's composition root (its <see cref="IModule"/>). It registers its use cases, the
/// synchronous public API other modules call (<see cref="IWarehouseApi"/>), the MediatR bus its
/// <c>OrderPlaced</c> handler lives on, and its seeder, and declares the <see cref="WarehouseDbContext"/>.
/// </summary>
public sealed class WarehouseModule : IModule
{
    public string Name => "Warehouse";
    public Type ContextType => typeof(WarehouseDbContext);

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        // This module's controllers, registered as an MVC application part. The host turns OFF the SDK's
        // implicit controller discovery (GenerateMvcApplicationPartsAssemblyAttributes=false), so every module
        // MUST add its own Api assembly explicitly here — see docs/decision-log.md D13.
        services.AddControllers().AddApplicationPart(typeof(ProductsController).Assembly);

        services.AddUseCases(typeof(DecrementStockUseCase).Assembly);

        // Synchronous public API other modules call (they depend only on IWarehouseApi).
        services.AddScoped<IWarehouseApi, WarehouseApi>();

        // Warehouse subscribes to OrderPlaced — its INotificationHandler lives in this assembly, so it
        // registers MediatR over its own assembly.
        services.AddMediatR(cfg =>
        {
            var licenseKey = configuration["MediatR:LicenseKey"];
            if (!string.IsNullOrWhiteSpace(licenseKey))
                cfg.LicenseKey = licenseKey;
            cfg.RegisterServicesFromAssembly(typeof(WarehouseModule).Assembly);
        });

        services.AddScoped<IModuleInitializer, WarehouseSeeder>();
    }
}
