// Placed in the Microsoft.EntityFrameworkCore namespace (like EF's own Use* methods) so it is
// discoverable wherever a module configures its DbContext, without an extra using directive.
namespace Microsoft.EntityFrameworkCore;

/// <summary>
/// Configures a module's DbContext for SQL Server AND places its EF migrations-history table in
/// the module's OWN schema. The upshot: each module fully owns its schema — including the
/// bookkeeping of which migrations have been applied — so nothing is shared in the default
/// <c>dbo</c> schema. This makes the data-isolation boundary complete.
/// </summary>
public static class ModuleDbContextOptions
{
    // Used by AddDbContext(...) in a module's Register method.
    public static DbContextOptionsBuilder UseModuleSqlServer(
        this DbContextOptionsBuilder options, string? connectionString, string schema)
        => options.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", schema));

    // Used by the design-time factories, which build a typed DbContextOptions<TContext>.
    public static DbContextOptionsBuilder<TContext> UseModuleSqlServer<TContext>(
        this DbContextOptionsBuilder<TContext> options, string? connectionString, string schema)
        where TContext : DbContext
        => options.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", schema));
}
