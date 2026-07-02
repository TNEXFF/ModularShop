using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModularShop.Modules.Shipping.Domain;

namespace ModularShop.Modules.Shipping.Infrastructure.Persistence.Configurations;

internal sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("Shipments");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ShipmentNumber).HasMaxLength(32).IsRequired();
        builder.HasIndex(s => s.ShipmentNumber).IsUnique();
        builder.Property(s => s.OrderNumber).HasMaxLength(32).IsRequired();
        builder.HasIndex(s => s.OrderId);
        builder.Property(s => s.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Carrier).HasMaxLength(100);
        builder.Property(s => s.TrackingNumber).HasMaxLength(100);
        builder.Ignore(s => s.TotalUnits);

        builder.HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey(i => i.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
