using Microsoft.EntityFrameworkCore;
using ModularShop.Kernel.Domain;
using ModularShop.Kernel.Infrastructure.Persistence;
using ModularShop.Modules.Support.Domain;

namespace ModularShop.Modules.Support.Infrastructure.Persistence;

/// <summary>
/// The Support module's DbContext. It declares the module's entities and configures them — and their
/// <c>support</c> schema — here. The host instantiates it only to layer this model onto the single host
/// context (see <see cref="ModuleDbContext"/>); it is never registered or connected at runtime.
/// </summary>
public sealed class SupportDbContext : ModuleDbContext
{
    public const string Schema = "support";

    public SupportDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(ticket =>
        {
            ticket.ToTable("Tickets", Schema);
            ticket.Property(t => t.Subject).HasMaxLength(200).IsRequired();
            ticket.Property(t => t.Description).HasMaxLength(4000);
            ticket.Property(t => t.CustomerName).HasMaxLength(200).IsRequired();
            ticket.Property(t => t.CreatedByName).HasMaxLength(200).IsRequired();
            ticket.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            // CreatedByUserId is a Guid (the Identity key) — stored as-is, no length configuration needed.

            // FK to the SHARED kernel Customer (cross-schema: support.Tickets → kernel.Customers).
            ticket.HasOne<Customer>().WithMany().HasForeignKey(t => t.CustomerId).OnDelete(DeleteBehavior.Restrict);

            ticket.HasMany(t => t.Messages).WithOne().HasForeignKey(m => m.TicketId).OnDelete(DeleteBehavior.Cascade);
            ticket.Navigation(t => t.Messages).UsePropertyAccessMode(PropertyAccessMode.Field);

            ticket.HasIndex(t => t.CustomerId);
            ticket.HasIndex(t => t.Status);
        });

        modelBuilder.Entity<TicketMessage>(message =>
        {
            message.ToTable("TicketMessages", Schema); // child entity (not a DbSet)
            message.Property(m => m.AuthorName).HasMaxLength(200).IsRequired();
            message.Property(m => m.Body).HasMaxLength(4000).IsRequired();
            // AuthorUserId is a Guid (the Identity key) — stored as-is, no length configuration needed.
        });
    }
}
