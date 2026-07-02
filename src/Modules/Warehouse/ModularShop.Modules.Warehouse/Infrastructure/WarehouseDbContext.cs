using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Warehouse.Domain;
using ModularShop.SharedKernel.Infrastructure.Persistence;

namespace ModularShop.Modules.Warehouse.Infrastructure;

/// <summary>
/// The Warehouse module's own DbContext. It derives from <see cref="ModuleDbContext"/>, so all its
/// tables live in the <c>warehouse</c> schema. No other module shares or references this context.
/// </summary>
internal sealed class WarehouseDbContext : ModuleDbContext
{
    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options) { }

    protected override string Schema => "warehouse";

    public DbSet<Product> Products => Set<Product>();
}
