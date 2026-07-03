using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Shipping.Domain;

namespace ModularShop.Modules.Shipping.Infrastructure.Persistence;

/// <summary>
/// The Shipping module's <b>blueprint</b> DbContext — never instantiated at runtime. Its <c>DbSet</c>
/// declares the entities the module owns; the single host context reflects it to build one combined
/// model (see <c>IModuleModel</c>).
/// </summary>
internal sealed class ShippingDbContext : DbContext
{
    public ShippingDbContext(DbContextOptions<ShippingDbContext> options) : base(options) { }

    public DbSet<Shipment> Shipments => Set<Shipment>();
}
