using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Kernel.Infrastructure.Persistence;
using ModularShop.Modules.Shipping.Application;
using ModularShop.Modules.Shipping.Domain;
using ModularShop.Modules.Shipping.Infrastructure.Persistence;

namespace ModularShop.Modules.Shipping.Infrastructure;

/// <summary>
/// The Shipping module's composition root. Note this module exposes NO <c>.Contracts</c> project:
/// nothing else in the system calls into Shipping, so it needs no public API. It only consumes the
/// Sales <c>OrderPlaced</c> event (via its integration-event handler) and serves its own endpoints.
/// </summary>
public sealed class ShippingModule : IModule
{
    public string Name => "Shipping";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ModularShopDemo");
        services.AddDbContext<ShippingDbContext>(options => options.UseModuleSqlServer(connectionString, "shipping"));

        // Ardalis.Specification repository, bound to this module's DbContext (read + write).
        services.AddScoped<IRepositoryBase<Shipment>, EfRepository<Shipment, ShippingDbContext>>();
        services.AddScoped<IReadRepositoryBase<Shipment>, EfRepository<Shipment, ShippingDbContext>>();

        // Use cases — invoked by controllers and by the integration-event handler.
        services.AddScoped<ListShipments>();
        services.AddScoped<GetShipment>();
        services.AddScoped<ShipShipment>();
        services.AddScoped<DeliverShipment>();
        services.AddScoped<CreateShipment>();

        // Owns its schema + seed.
        services.AddScoped<IModuleInitializer, ShippingModuleInitializer>();
    }
}
