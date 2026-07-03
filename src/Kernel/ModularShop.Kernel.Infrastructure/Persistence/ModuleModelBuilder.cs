using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ModularShop.Kernel.Domain;

namespace ModularShop.Kernel.Infrastructure.Persistence;

/// <summary>
/// Turns a set of <see cref="IModuleModel"/> contributions into one EF Core model. This is the heart of
/// the "one host context + one context-per-module blueprint" design: the host owns no entity knowledge
/// itself — it just asks every module to contribute, then assigns schemas.
/// </summary>
public static class ModuleModelBuilder
{
    /// <summary>
    /// Registers every entity a module exposes as a <c>DbSet&lt;T&gt;</c> on its blueprint context —
    /// the table name is taken from the DbSet property name — and then applies the module's own special
    /// configuration. The DbSets are the "recipe"; <see cref="IModuleModel.Configure"/> is only for what
    /// they can't express.
    /// </summary>
    public static void ApplyModuleModel(this ModelBuilder modelBuilder, IModuleModel module)
    {
        foreach (var (tableName, entityType) in DbSetEntities(module.ContextType))
            modelBuilder.Entity(entityType).ToTable(tableName);

        module.Configure(modelBuilder);
    }

    /// <summary>
    /// Places each entity in its owning module's schema. Ownership is decided by the CLR assembly the
    /// entity type lives in (all of a module's entities live in its Domain assembly), so child entities
    /// reached only through a navigation — <c>OrderLine</c>, <c>ShipmentItem</c>, <c>TicketMessage</c> —
    /// are placed correctly too, without being listed anywhere. Anything not owned by a module (the
    /// shared kernel entities and every Identity table) falls into <paramref name="kernelSchema"/>.
    /// </summary>
    public static void ApplyModuleSchemas(
        this ModelBuilder modelBuilder, IEnumerable<IModuleModel> modules, string kernelSchema)
    {
        var schemaByAssembly = new Dictionary<Assembly, string>();
        foreach (var module in modules)
            foreach (var (_, entityType) in DbSetEntities(module.ContextType))
                schemaByAssembly[entityType.Assembly] = module.Schema;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var owningSchema = schemaByAssembly.TryGetValue(entityType.ClrType.Assembly, out var schema)
                ? schema
                : kernelSchema;
            entityType.SetSchema(owningSchema);
        }
    }

    /// <summary>
    /// Marks every domain entity's key as <b>client-assigned</b> (<see cref="ValueGenerated.Never"/>).
    /// Each <see cref="Entity"/> sets its own <see cref="Entity.Id"/> Guid in its constructor, but EF Core's
    /// default Guid convention is <c>ValueGeneratedOnAdd</c>. With that default, a NEW child added to an
    /// ALREADY-TRACKED parent (e.g. a <c>TicketMessage</c> added to a loaded <c>Ticket</c>) is mis-detected
    /// as an existing row — EF issues an <c>UPDATE</c> that affects 0 rows and throws a concurrency error
    /// instead of inserting. Telling EF the key is client-assigned fixes that. The column is identical
    /// either way (the Guid is produced in-memory, never by the database), so this changes no migration.
    /// Only <see cref="Entity"/>-derived types are touched — Identity's string/int keys and the
    /// code-keyed <c>Currency</c> keep their own conventions.
    /// </summary>
    public static void ApplyClientAssignedKeys(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(Entity).IsAssignableFrom(entityType.ClrType))
                continue;

            var idProperty = entityType.FindProperty(nameof(Entity.Id));
            if (idProperty is not null)
                idProperty.ValueGenerated = ValueGenerated.Never;
        }
    }

    /// <summary>Reflects a blueprint context's public <c>DbSet&lt;T&gt;</c> properties into (table name, entity type) pairs.</summary>
    private static IEnumerable<(string TableName, Type EntityType)> DbSetEntities(Type contextType) =>
        contextType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType
                        && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => (p.Name, p.PropertyType.GetGenericArguments()[0]));
}
