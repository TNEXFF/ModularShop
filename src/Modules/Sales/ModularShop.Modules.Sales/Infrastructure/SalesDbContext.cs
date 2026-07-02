using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Sales.Domain;
using ModularShop.SharedKernel.Infrastructure.Persistence;

namespace ModularShop.Modules.Sales.Infrastructure;

/// <summary>The Sales module's own DbContext — all its tables live in the <c>sales</c> schema.</summary>
internal sealed class SalesDbContext : ModuleDbContext
{
    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options) { }

    protected override string Schema => "sales";

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
}
