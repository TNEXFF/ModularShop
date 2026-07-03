using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Warehouse.Domain;

namespace ModularShop.Modules.Warehouse.Infrastructure.Persistence;

/// <summary>
/// The Warehouse module's <b>blueprint</b> DbContext — never instantiated at runtime. Its <c>DbSet</c>
/// properties declare the entities the module owns; the single host context reflects them to build one
/// combined model (see <c>IModuleModel</c>).
/// </summary>
internal sealed class WarehouseDbContext : DbContext
{
    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
}
