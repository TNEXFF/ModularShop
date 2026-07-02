using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Kernel.Infrastructure.Persistence;
using ModularShop.Modules.Warehouse.Application;
using ModularShop.Modules.Warehouse.Contracts;
using ModularShop.Modules.Warehouse.Domain;
using ModularShop.Modules.Warehouse.Infrastructure.Persistence;

namespace ModularShop.Modules.Warehouse.Infrastructure;

/// <summary>
/// The Warehouse module's composition root. It registers everything the module needs (its own
/// DbContext, repositories, use cases, public API and initializer). The integration-event handler
/// in this assembly is discovered by MediatR in the host, which scans each module's Infrastructure
/// assembly.
/// </summary>
public sealed class WarehouseModule : IModule
{
    public string Name => "Warehouse";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ModularShopDemo");
        services.AddDbContext<WarehouseDbContext>(options => options.UseModuleSqlServer(connectionString, "warehouse"));

        // Ardalis.Specification repository, bound to this module's DbContext (read + write).
        services.AddScoped<IRepositoryBase<Product>, EfRepository<Product, WarehouseDbContext>>();
        services.AddScoped<IReadRepositoryBase<Product>, EfRepository<Product, WarehouseDbContext>>();

        // Use cases — invoked by controllers and by the integration-event handler.
        services.AddScoped<ListProducts>();
        services.AddScoped<GetProduct>();
        services.AddScoped<DecrementStock>();

        // Synchronous public API other modules call (they depend only on IWarehouseApi).
        services.AddScoped<IWarehouseApi, WarehouseApi>();

        // Owns its schema + seed.
        services.AddScoped<IModuleInitializer, WarehouseModuleInitializer>();
    }
}
