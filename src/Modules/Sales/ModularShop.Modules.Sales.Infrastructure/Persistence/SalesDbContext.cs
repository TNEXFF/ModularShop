using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Sales.Domain;

namespace ModularShop.Modules.Sales.Infrastructure.Persistence;

/// <summary>
/// The Sales module's <b>blueprint</b> DbContext. It is never instantiated at runtime — its only job is
/// to declare, through <c>DbSet</c> properties, which entities the module owns. The single host context
/// reflects these DbSets (see <c>IModuleModel</c>) to build one combined model. Keeping a context per
/// module gives each module an obvious, self-contained place to declare its slice of the schema, and
/// makes "which tables are mine" answerable at a glance.
/// </summary>
internal sealed class SalesDbContext : DbContext
{
    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
}
