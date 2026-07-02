using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularShop.Modules.Sales.Contracts;
using ModularShop.Modules.Warehouse.Api;
using ModularShop.Modules.Warehouse.Application;
using ModularShop.Modules.Warehouse.Application.IntegrationEventHandlers;
using ModularShop.Modules.Warehouse.Contracts;
using ModularShop.Modules.Warehouse.Infrastructure;
using ModularShop.SharedKernel.Infrastructure;
using ModularShop.SharedKernel.Messaging;
using ModularShop.SharedKernel.Web;

namespace ModularShop.Modules.Warehouse;

/// <summary>
/// The Warehouse module's composition root. It registers everything the module needs (its own
/// DbContext, services, public API, event handler, initializer) and maps its endpoints. This is
/// the ONLY public type in the module — everything else is <c>internal</c>.
/// </summary>
public sealed class WarehouseModule : IModule
{
    public string Name => "Warehouse";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ModularShopDemo");
        services.AddDbContext<WarehouseDbContext>(options => options.UseModuleSqlServer(connectionString, "warehouse"));

        services.AddScoped<ProductService>();

        // Synchronous public API other modules call (they depend only on IWarehouseApi).
        services.AddScoped<IWarehouseApi, WarehouseApi>();

        // Asynchronous reaction to another module's event.
        services.AddScoped<IIntegrationEventHandler<OrderPlaced>, DecrementStockOnOrderPlaced>();

        // Owns its schema + seed.
        services.AddScoped<IModuleInitializer, WarehouseModuleInitializer>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapWarehouseEndpoints();
}
