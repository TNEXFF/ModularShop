using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Kernel.Infrastructure.Persistence;
using ModularShop.Modules.Sales.Application;
using ModularShop.Modules.Sales.Domain;
using ModularShop.Modules.Sales.Infrastructure.Persistence;

namespace ModularShop.Modules.Sales.Infrastructure;

/// <summary>
/// The Sales module's composition root. It registers the module's DbContext, its repositories
/// (Ardalis.Specification, bound to this module's context), its use cases and its initializer.
/// Sales publishes the <c>OrderPlaced</c> integration event but handles none, so it registers no
/// event handlers.
/// </summary>
public sealed class SalesModule : IModule
{
    public string Name => "Sales";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ModularShopDemo");
        services.AddDbContext<SalesDbContext>(options => options.UseModuleSqlServer(connectionString, "sales"));

        // Ardalis.Specification repositories, bound to this module's DbContext.
        services.AddScoped<IRepositoryBase<Order>, EfRepository<Order, SalesDbContext>>();
        services.AddScoped<IReadRepositoryBase<Order>, EfRepository<Order, SalesDbContext>>();
        services.AddScoped<IReadRepositoryBase<Customer>, EfRepository<Customer, SalesDbContext>>();

        // Use cases (invoked by the controllers).
        services.AddScoped<GetOrders>();
        services.AddScoped<GetOrder>();
        services.AddScoped<PlaceOrder>();
        services.AddScoped<ListCustomers>();

        // Owns its schema + seed.
        services.AddScoped<IModuleInitializer, SalesModuleInitializer>();
    }
}
