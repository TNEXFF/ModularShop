using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Shipping.Domain;
using ModularShop.SharedKernel.Infrastructure.Persistence;

namespace ModularShop.Modules.Shipping.Infrastructure;

/// <summary>The Shipping module's own DbContext — all its tables live in the <c>shipping</c> schema.</summary>
internal sealed class ShippingDbContext : ModuleDbContext
{
    public ShippingDbContext(DbContextOptions<ShippingDbContext> options) : base(options) { }

    protected override string Schema => "shipping";

    public DbSet<Shipment> Shipments => Set<Shipment>();
}
