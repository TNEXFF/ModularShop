using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Modules.Sales.Api.Controllers;
using ModularShop.Modules.Sales.Application.UseCases;
using ModularShop.Modules.Sales.Infrastructure.Persistence;

namespace ModularShop.Modules.Sales.Infrastructure;

/// <summary>
/// The Sales module's composition root (its <see cref="IModule"/>). It registers ALL of its own parts —
/// every use case (by scanning its Application assembly for <c>UseCase</c> subclasses), the MediatR bus it
/// publishes <c>OrderPlaced</c> on, and its seeder — and declares the <see cref="SalesDbContext"/> it
/// contributes to the one host model. Its controllers (in the Sales.Api assembly) are registered as an MVC
/// application part in <see cref="Register"/>.
/// </summary>
public sealed class SalesModule : IModule
{
    public string Name => "Sales";
    public Type ContextType => typeof(SalesDbContext);

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        // This module's controllers, registered as an MVC application part. The host turns OFF the SDK's
        // implicit controller discovery (GenerateMvcApplicationPartsAssemblyAttributes=false), so every module
        // MUST add its own Api assembly explicitly here — see docs/decision-log.md D13.
        services.AddControllers().AddApplicationPart(typeof(OrdersController).Assembly);

        // Every use case in the Sales Application assembly (each inherits UseCase), by convention.
        services.AddUseCases(typeof(PlaceOrderUseCase).Assembly);

        // Sales publishes the OrderPlaced integration event, so it registers MediatR over its OWN assembly
        // (it has no handlers itself — Warehouse and Shipping register theirs the same way).
        services.AddMediatR(cfg =>
        {
            var licenseKey = configuration["MediatR:LicenseKey"];
            if (!string.IsNullOrWhiteSpace(licenseKey))
                cfg.LicenseKey = licenseKey;
            cfg.RegisterServicesFromAssembly(typeof(SalesModule).Assembly);
        });

        // Seeds its historical orders at startup (the kernel seeds customers first).
        services.AddScoped<IModuleInitializer, SalesSeeder>();
    }
}
