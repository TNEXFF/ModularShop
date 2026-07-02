using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularShop.Modules.Sales.Api;
using ModularShop.Modules.Sales.Application;
using ModularShop.Modules.Sales.Domain;
using ModularShop.Modules.Sales.Infrastructure;
using ModularShop.SharedKernel.Infrastructure;
using ModularShop.SharedKernel.Infrastructure.Persistence;
using ModularShop.SharedKernel.Persistence;
using ModularShop.SharedKernel.Web;

namespace ModularShop.Modules.Sales;

/// <summary>
/// The Sales module's composition root — the only public type in the module. It registers the
/// module's DbContext, its order repository (demonstrating the repository pattern), its services,
/// and its initializer, then maps its endpoints.
/// </summary>
public sealed class SalesModule : IModule
{
    public string Name => "Sales";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ModularShopDemo");
        services.AddDbContext<SalesDbContext>(options => options.UseModuleSqlServer(connectionString, "sales"));

        // Repository pattern (adopted, simplified, from the Platform), bound to this module's context.
        services.AddScoped<IRepository<Order>, EfRepository<Order, SalesDbContext>>();

        services.AddScoped<OrderService>();
        services.AddScoped<CustomerService>();
        services.AddScoped<IModuleInitializer, SalesModuleInitializer>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapSalesEndpoints();
}
