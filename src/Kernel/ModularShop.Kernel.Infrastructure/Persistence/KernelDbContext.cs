using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ModularShop.Kernel.Domain;
using ModularShop.Kernel.Domain.Identity;

namespace ModularShop.Kernel.Infrastructure.Persistence;

/// <summary>
/// The kernel's DbContext. The kernel is composed into the single host model exactly like any other
/// module — the host harvests this model by reflection. It brings in ASP.NET Core Identity (users,
/// roles, claims…) keyed by <b>Guid</b> — matching every other entity in the system — and the shared
/// kernel entities (<see cref="Customer"/>, <see cref="Currency"/>) that several modules reference. It
/// is never registered in DI or connected to a database at runtime: the host instantiates it only to
/// harvest its model. Everything the kernel owns lives in the <c>kernel</c> schema (the model default it
/// sets below); each module overrides that default for its own tables.
/// </summary>
public sealed class KernelDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    /// <summary>Schema for everything the kernel owns (shared entities + all Identity tables).</summary>
    public const string Schema = "kernel";

    public KernelDbContext(DbContextOptions options) : base(options) { }

    // Explicit DbSets for the kernel's root entities — the Identity users/roles are first-class kernel
    // tables, declared here just as a module declares its own (the base type also exposes Users/Roles).
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<ApplicationRole> ApplicationRoles => Set<ApplicationRole>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Currency> Currencies => Set<Currency>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Everything the kernel owns (Identity tables + shared entities) defaults to the kernel schema.
        // Feature modules override this default for their own tables via ToTable(name, schema).
        modelBuilder.HasDefaultSchema(Schema);

        base.OnModelCreating(modelBuilder); // Identity entity mappings (AspNetUsers, AspNetRoles, …)

        modelBuilder.Entity<ApplicationUser>().Property(u => u.DisplayName).HasMaxLength(200);

        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("Customers");
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.Property(c => c.Email).HasMaxLength(256).IsRequired();
            e.HasIndex(c => c.Email).IsUnique();
        });

        modelBuilder.Entity<Currency>(e =>
        {
            e.ToTable("Currencies");
            e.HasKey(c => c.Code);
            e.Property(c => c.Code).HasMaxLength(3).IsRequired();
            e.Property(c => c.Symbol).HasMaxLength(4).IsRequired();
            e.Property(c => c.Name).HasMaxLength(50).IsRequired();
        });
    }
}
