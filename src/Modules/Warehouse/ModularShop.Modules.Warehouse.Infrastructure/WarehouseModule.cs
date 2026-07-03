using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularShop.Kernel.Domain;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Kernel.Infrastructure.Persistence;
using ModularShop.Modules.Warehouse.Application;
using ModularShop.Modules.Warehouse.Contracts;
using ModularShop.Modules.Warehouse.Domain;
using ModularShop.Modules.Warehouse.Infrastructure.Persistence;

namespace ModularShop.Modules.Warehouse.Infrastructure;

/// <summary>
/// The Warehouse module's composition root — both <see cref="IModule"/> (its own services) and
/// <see cref="IModuleModel"/> (its slice of the single host model). The integration-event handler in
/// this assembly is discovered by MediatR when the host scans each module's Infrastructure assembly.
/// </summary>
public sealed class WarehouseModule : IModule, IModuleModel
{
    public string Name => "Warehouse";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        // Use cases — invoked by controllers and by the integration-event handler.
        services.AddScoped<ListProducts>();
        services.AddScoped<GetProduct>();
        services.AddScoped<DecrementStock>();

        // Synchronous public API other modules call (they depend only on IWarehouseApi).
        services.AddScoped<IWarehouseApi, WarehouseApi>();

        // Seeds the product catalogue at startup.
        services.AddScoped<IModuleInitializer, WarehouseSeeder>();
    }

    public string Schema => "warehouse";
    public Type ContextType => typeof(WarehouseDbContext);

    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(product =>
        {
            product.Property(p => p.Sku).HasMaxLength(32).IsRequired();
            product.HasIndex(p => p.Sku).IsUnique();
            product.Property(p => p.Name).HasMaxLength(200).IsRequired();
            product.Property(p => p.Description).HasMaxLength(1000).IsRequired();
            product.Property(p => p.Category).HasMaxLength(100).IsRequired();
            product.Property(p => p.Price).HasPrecision(18, 2);
            product.Property(p => p.CurrencyCode).HasMaxLength(3).IsRequired();

            // FK to the SHARED kernel Currency (cross-schema: warehouse.Products → kernel.Currencies).
            product.HasOne<Currency>().WithMany().HasForeignKey(p => p.CurrencyCode).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
