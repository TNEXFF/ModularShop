using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ModularShop.Modules.Warehouse.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build this module's DbContext without booting the
/// host. The connection string here is used ONLY by the migration tooling; at runtime the module
/// reads it from configuration. Giving each module its own factory is what lets each module own
/// (and generate) its migrations independently.
/// </summary>
internal sealed class WarehouseDbContextFactory : IDesignTimeDbContextFactory<WarehouseDbContext>
{
    public WarehouseDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("MODULARSHOP_CS")
            ?? "Server=localhost;Database=ModularShopDemo;Trusted_Connection=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseModuleSqlServer(connectionString, "warehouse")
            .Options;
        return new WarehouseDbContext(options);
    }
}
