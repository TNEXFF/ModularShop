using Microsoft.EntityFrameworkCore;
using ModularShop.Kernel.Domain;
using ModularShop.Kernel.Infrastructure.Persistence;
using ModularShop.Modules.Sales.Domain;

namespace ModularShop.Modules.Sales.Infrastructure.Persistence;

/// <summary>
/// The Sales module's DbContext. It declares the module's entities (as <c>DbSet</c>s) and configures them
/// — and their <c>sales</c> schema — in <see cref="OnModelCreating"/>, exactly like a standalone context.
/// The host never registers or connects it; it instantiates it only to layer this model onto the single
/// host context (see <see cref="ModuleDbContext"/>). This is the module's one and only place for EF config.
/// </summary>
public sealed class SalesDbContext : ModuleDbContext
{
    public const string Schema = "sales";

    public SalesDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(order =>
        {
            order.ToTable("Orders", Schema);
            order.Property(o => o.OrderNumber).HasMaxLength(32).IsRequired();
            order.HasIndex(o => o.OrderNumber).IsUnique();
            order.Property(o => o.CustomerName).HasMaxLength(200).IsRequired();
            order.Property(o => o.CurrencyCode).HasMaxLength(3).IsRequired();
            order.Property(o => o.PlacedBy).HasMaxLength(100).IsRequired();
            order.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            order.Ignore(o => o.Total); // computed from lines, not stored

            // Foreign keys to the SHARED KERNEL entities (cross-schema, e.g. sales.Orders → kernel.Customers).
            // No navigation property: the FK gives referential integrity while the order stays snapshot-based.
            order.HasOne<Customer>().WithMany().HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
            order.HasOne<Currency>().WithMany().HasForeignKey(o => o.CurrencyCode).OnDelete(DeleteBehavior.Restrict);

            // One order has many lines. EF binds the read-only Lines navigation to the _lines field.
            order.HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);
            order.Navigation(o => o.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

            order.HasIndex(o => o.CustomerId);
            order.HasIndex(o => o.Status);
        });

        modelBuilder.Entity<OrderLine>(line =>
        {
            line.ToTable("OrderLines", Schema); // child entity (not a DbSet)
            line.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
            line.Property(l => l.UnitPrice).HasPrecision(18, 2);
            line.Ignore(l => l.LineTotal); // computed
        });
    }
}
