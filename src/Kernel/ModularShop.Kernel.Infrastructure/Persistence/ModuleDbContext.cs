using Microsoft.EntityFrameworkCore;

namespace ModularShop.Kernel.Infrastructure.Persistence;

/// <summary>
/// Base class for a feature module's DbContext. A module context declares its entities (as
/// <c>DbSet</c>s) and configures them — including their schema — in <c>OnModelCreating</c>, exactly like
/// an ordinary standalone context. It is never registered in DI or connected to a database at runtime:
/// the single host context instantiates it only to harvest its model through <see cref="ApplyModel"/>.
/// <para>
/// The kernel is the one exception: it must be an <c>IdentityDbContext</c>, so it implements
/// <see cref="IModelContributor"/> directly instead of deriving from here — but it is composed by the
/// host in exactly the same way, as just another (special) module.
/// </para>
/// </summary>
public abstract class ModuleDbContext : DbContext, IModelContributor
{
    protected ModuleDbContext(DbContextOptions options) : base(options) { }

    /// <summary>Surfaces the context's own <c>OnModelCreating</c> so the host can layer it onto the one shared model.</summary>
    public void ApplyModel(ModelBuilder modelBuilder) => OnModelCreating(modelBuilder);
}
