using Microsoft.EntityFrameworkCore;
using ModularShop.Kernel.Domain;
using ModularShop.Modules.Shipping.Domain;

namespace ModularShop.Modules.Shipping.Infrastructure.Persistence;

/// <summary>
/// The Shipping module's DbContext. It declares the module's entities and configures them — and their
/// <c>shipping</c> schema — here. The host instantiates it only to layer this model onto the single host
/// context (the host harvests this model by reflection); it is never registered or connected at runtime.
/// </summary>
public sealed class ShippingDbContext : DbContext
{
    public const string Schema = "shipping";

    public ShippingDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Shipment> Shipments => Set<Shipment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shipment>(shipment =>
        {
            shipment.ToTable("Shipments", Schema);
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
            item.ToTable("ShipmentItems", Schema); // child entity (not a DbSet)
            item.Property(i => i.ProductName).HasMaxLength(200).IsRequired();
        });
    }
}
