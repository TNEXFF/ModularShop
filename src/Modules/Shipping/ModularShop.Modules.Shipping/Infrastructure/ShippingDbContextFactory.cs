using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ModularShop.Modules.Shipping.Infrastructure;

/// <summary>Design-time factory so <c>dotnet ef</c> can build this module's DbContext independently.</summary>
internal sealed class ShippingDbContextFactory : IDesignTimeDbContextFactory<ShippingDbContext>
{
    public ShippingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("MODULARSHOP_CS")
            ?? "Server=localhost;Database=ModularShopDemo;Trusted_Connection=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ShippingDbContext>()
            .UseModuleSqlServer(connectionString, "shipping")
            .Options;
        return new ShippingDbContext(options);
    }
}
