using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularShop.Kernel.Domain;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Kernel.Infrastructure.Persistence;
using ModularShop.Modules.Sales.Application;
using ModularShop.Modules.Sales.Domain;
using ModularShop.Modules.Sales.Infrastructure.Persistence;

namespace ModularShop.Modules.Sales.Infrastructure;

/// <summary>
/// The Sales module's composition root. It implements <see cref="IModule"/> (register the module's own
/// services) AND <see cref="IModuleModel"/> (contribute the module's entities + mapping to the single
/// host model). Sales publishes the <c>OrderPlaced</c> integration event but handles none, so it
/// registers no event handlers.
/// </summary>
public sealed class SalesModule : IModule, IModuleModel
{
    public string Name => "Sales";

    // ── IModule: the module's own services (no DbContext — the host owns the one context) ──────────
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<GetOrders>();
        services.AddScoped<GetOrder>();
        services.AddScoped<PlaceOrder>();
        services.AddScoped<ListCustomers>();

        // Seeds its historical orders at startup (the kernel seeds customers first).
        services.AddScoped<IModuleInitializer, SalesSeeder>();
    }

    // ── IModuleModel: this module's slice of the single host model ─────────────────────────────────
    public string Schema => "sales";
    public Type ContextType => typeof(SalesDbContext); // its DbSet<Order> declares the Sales entities

    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(order =>
        {
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
            line.ToTable("OrderLines"); // child entity (not a DbSet), so name it here
            line.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
            line.Property(l => l.UnitPrice).HasPrecision(18, 2);
            line.Ignore(l => l.LineTotal); // computed
        });
    }
}
