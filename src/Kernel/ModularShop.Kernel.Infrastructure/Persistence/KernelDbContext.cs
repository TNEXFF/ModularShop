using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ModularShop.Kernel.Domain;
using ModularShop.Kernel.Infrastructure.Identity;

namespace ModularShop.Kernel.Infrastructure.Persistence;

/// <summary>
/// The kernel's DbContext base. It brings in ASP.NET Core Identity (users, roles, claims…) and the
/// <b>shared kernel entities</b> (<see cref="Customer"/>, <see cref="Currency"/>) that several modules
/// reference. The single host context derives from this and then layers each module's model on top
/// (see <c>IModuleModel</c>), so everything the kernel owns is available to every module through one
/// context. This class is abstract — only the host context is ever instantiated.
/// </summary>
public abstract class KernelDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    protected KernelDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Currency> Currencies => Set<Currency>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

        // NB: the schema for these kernel-owned tables (and the Identity tables) is assigned by the host
        // context via ApplyModuleSchemas — anything not owned by a module falls into the "kernel" schema.
    }
}
