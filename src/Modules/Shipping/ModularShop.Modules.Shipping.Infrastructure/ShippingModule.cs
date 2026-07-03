using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularShop.Kernel.Domain;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Kernel.Infrastructure.Persistence;
using ModularShop.Modules.Shipping.Application;
using ModularShop.Modules.Shipping.Domain;
using ModularShop.Modules.Shipping.Infrastructure.Persistence;

namespace ModularShop.Modules.Shipping.Infrastructure;

/// <summary>
/// The Shipping module's composition root — both <see cref="IModule"/> and <see cref="IModuleModel"/>.
/// It exposes NO <c>.Contracts</c> project: nothing else calls into Shipping. It only consumes the Sales
/// <c>OrderPlaced</c> event (via its integration-event handler) and serves its own endpoints.
/// </summary>
public sealed class ShippingModule : IModule, IModuleModel
{
    public string Name => "Shipping";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ListShipments>();
        services.AddScoped<GetShipment>();
        services.AddScoped<ShipShipment>();
        services.AddScoped<DeliverShipment>();
        services.AddScoped<CreateShipment>();

        services.AddScoped<IModuleInitializer, ShippingSeeder>();
    }

    public string Schema => "shipping";
    public Type ContextType => typeof(ShippingDbContext);

    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shipment>(shipment =>
        {
            shipment.Property(s => s.ShipmentNumber).HasMaxLength(32).IsRequired();
            shipment.HasIndex(s => s.ShipmentNumber).IsUnique();
            shipment.Property(s => s.OrderNumber).HasMaxLength(32).IsRequired();
            shipment.HasIndex(s => s.OrderId);
            shipment.Property(s => s.CustomerName).HasMaxLength(200).IsRequired();
            shipment.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
            shipment.Property(s => s.Carrier).HasMaxLength(100);
            shipment.Property(s => s.TrackingNumber).HasMaxLength(100);
            shipment.Ignore(s => s.TotalUnits);

            // FK to the SHARED kernel Customer (cross-schema: shipping.Shipments → kernel.Customers).
            shipment.HasOne<Customer>().WithMany().HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.Restrict);

            shipment.HasMany(s => s.Items).WithOne().HasForeignKey(i => i.ShipmentId).OnDelete(DeleteBehavior.Cascade);
            shipment.Navigation(s => s.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ShipmentItem>(item =>
        {
            item.ToTable("ShipmentItems"); // child entity (not a DbSet)
            item.Property(i => i.ProductName).HasMaxLength(200).IsRequired();
        });
    }
}
