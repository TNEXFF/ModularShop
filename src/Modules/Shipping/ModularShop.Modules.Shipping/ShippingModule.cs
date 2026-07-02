using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularShop.Modules.Sales.Contracts;
using ModularShop.Modules.Shipping.Api;
using ModularShop.Modules.Shipping.Application;
using ModularShop.Modules.Shipping.Application.IntegrationEventHandlers;
using ModularShop.Modules.Shipping.Infrastructure;
using ModularShop.SharedKernel.Infrastructure;
using ModularShop.SharedKernel.Messaging;
using ModularShop.SharedKernel.Web;

namespace ModularShop.Modules.Shipping;

/// <summary>
/// The Shipping module's composition root. Note this module exposes NO <c>.Contracts</c> project:
/// nothing else in the system calls into Shipping, so it needs no public API. It only consumes the
/// Sales <c>OrderPlaced</c> event and serves its own endpoints.
/// </summary>
public sealed class ShippingModule : IModule
{
    public string Name => "Shipping";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ModularShopDemo");
        services.AddDbContext<ShippingDbContext>(options => options.UseModuleSqlServer(connectionString, "shipping"));

        services.AddScoped<ShipmentService>();

        // Asynchronous reaction to the Sales module's event.
        services.AddScoped<IIntegrationEventHandler<OrderPlaced>, CreateShipmentOnOrderPlaced>();

        services.AddScoped<IModuleInitializer, ShippingModuleInitializer>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapShippingEndpoints();
}
