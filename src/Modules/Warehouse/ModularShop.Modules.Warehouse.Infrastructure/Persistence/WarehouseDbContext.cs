using Microsoft.EntityFrameworkCore;
using ModularShop.Kernel.Domain;
using ModularShop.Modules.Warehouse.Domain;

namespace ModularShop.Modules.Warehouse.Infrastructure.Persistence;

/// <summary>
/// The Warehouse module's DbContext. It declares the module's entities and configures them — and their
/// <c>warehouse</c> schema — here. The host instantiates it only to layer this model onto the single host
/// context (the host harvests this model by reflection); it is never registered or connected at runtime.
/// </summary>
public sealed class WarehouseDbContext : DbContext
{
    public const string Schema = "warehouse";

    public WarehouseDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(product =>
        {
            product.ToTable("Products", Schema);
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
