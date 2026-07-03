using Microsoft.EntityFrameworkCore;

namespace ModularShop.Kernel.Infrastructure.Persistence;

/// <summary>
/// A module's contribution to the single host model. Each module implements this (on its <c>XModule</c>
/// class) so the host can build ONE combined DbContext out of every module without referencing any of
/// them directly. The host:
/// <list type="bullet">
/// <item>reflects <see cref="ContextType"/>'s <c>DbSet&lt;T&gt;</c> properties to register the module's
/// ordinary entities automatically (the "non-special" DbSets),</item>
/// <item>calls <see cref="Configure"/> for anything the plain DbSets can't express, and</item>
/// <item>places every table this module owns in <see cref="Schema"/>.</item>
/// </list>
/// This is the seam that lets "context-per-module" (as an organisational blueprint) coexist with a
/// single runtime context that owns migrations.
/// </summary>
public interface IModuleModel
{
    /// <summary>The MSSQL schema this module's tables live in (e.g. <c>"sales"</c>).</summary>
    string Schema { get; }

    /// <summary>
    /// The module's blueprint DbContext type. Its public <c>DbSet&lt;T&gt;</c> properties declare the
    /// module's root entities; the host reflects them so the module never has to list its entities twice.
    /// </summary>
    Type ContextType { get; }

    /// <summary>
    /// Special mapping the plain DbSets cannot express: relationships, indexes, value conversions,
    /// child-table names, and foreign keys to shared kernel entities. This is the module's one and only
    /// place for EF configuration — there are no per-entity configuration classes.
    /// </summary>
    void Configure(ModelBuilder modelBuilder);
}
