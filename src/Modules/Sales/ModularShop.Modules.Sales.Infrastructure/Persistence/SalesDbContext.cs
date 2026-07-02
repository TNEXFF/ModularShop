using Microsoft.EntityFrameworkCore;
using ModularShop.Kernel.Infrastructure.Persistence;
using ModularShop.Modules.Sales.Domain;

namespace ModularShop.Modules.Sales.Infrastructure.Persistence;

/// <summary>The Sales module's own DbContext — all its tables live in the <c>sales</c> schema.</summary>
internal sealed class SalesDbContext : ModuleDbContext
{
    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options) { }

    protected override string Schema => "sales";

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
}
