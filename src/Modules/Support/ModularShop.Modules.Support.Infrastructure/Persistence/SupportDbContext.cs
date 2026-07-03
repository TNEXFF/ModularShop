using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Support.Domain;

namespace ModularShop.Modules.Support.Infrastructure.Persistence;

/// <summary>
/// The Support module's <b>blueprint</b> DbContext — never instantiated at runtime. Its <c>DbSet</c>
/// declares the entities the module owns; the single host context reflects it to build one combined
/// model (see <c>IModuleModel</c>).
/// </summary>
internal sealed class SupportDbContext : DbContext
{
    public SupportDbContext(DbContextOptions<SupportDbContext> options) : base(options) { }

    public DbSet<Ticket> Tickets => Set<Ticket>();
}
