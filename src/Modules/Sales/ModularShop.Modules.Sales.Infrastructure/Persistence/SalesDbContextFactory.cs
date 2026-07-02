using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ModularShop.Modules.Sales.Infrastructure.Persistence;

/// <summary>Design-time factory so <c>dotnet ef</c> can build this module's DbContext independently.</summary>
internal sealed class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    public SalesDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("MODULARSHOP_CS")
            ?? "Server=localhost;Database=ModularShopDemo;Trusted_Connection=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseModuleSqlServer(connectionString, "sales")
            .Options;
        return new SalesDbContext(options);
    }
}
