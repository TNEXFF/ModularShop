using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ModularShop.Server.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef migrations add</c> can build the host context without starting
/// the whole app. It supplies the SAME set of modules the runtime uses (from <see cref="HostModules"/>),
/// so the single generated migration covers every module's tables — this is where <b>centralised
/// migrations</b> come from. The host, not the modules, owns the migration history.
/// </summary>
internal sealed class ModularShopDbContextFactory : IDesignTimeDbContextFactory<ModularShopDbContext>
{
    public ModularShopDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("MODULARSHOP_CS")
            ?? "Server=localhost;Database=ModularShopDemo;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<ModularShopDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo"))
            .Options;

        return new ModularShopDbContext(options, HostModules.All());
    }
}
