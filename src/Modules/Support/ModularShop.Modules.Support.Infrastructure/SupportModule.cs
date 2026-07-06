using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularShop.Kernel.Domain;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Kernel.Infrastructure.Persistence;
using ModularShop.Modules.Support.Application.Queries;
using ModularShop.Modules.Support.Application.UseCases;
using ModularShop.Modules.Support.Domain;
using ModularShop.Modules.Support.Infrastructure.Persistence;

namespace ModularShop.Modules.Support.Infrastructure;

/// <summary>
/// The Support module's composition root — both <see cref="IModule"/> and <see cref="IModuleModel"/>.
/// Support is a <b>genuinely unrelated</b> module: it publishes and consumes NO integration events and
/// calls into no other module. It only uses the kernel (the shared <c>Customer</c> + Identity), which is
/// exactly the point — an independent module can coexist and still share the kernel's cross-cutting parts.
/// </summary>
public sealed class SupportModule : IModule, IModuleModel
{
    public string Name => "Support";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ListTickets>();
        services.AddScoped<GetTicket>();
        services.AddScoped<CreateTicket>();
        services.AddScoped<AddTicketMessage>();
        services.AddScoped<ChangeTicketStatus>();

        // Support's specific read query (NOT a repository — the generic IRepository<Ticket> is
        // registered by the host and covers all entity CRUD). Used by ListTickets for its efficient
        // count-projection query, which a repository has no business returning.
        services.AddScoped<ITicketSummaryQuery, TicketSummaryQuery>();

        services.AddScoped<IModuleInitializer, SupportSeeder>();
    }

    public string Schema => "support";
    public Type ContextType => typeof(SupportDbContext);

    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(ticket =>
        {
            ticket.Property(t => t.Subject).HasMaxLength(200).IsRequired();
            ticket.Property(t => t.Description).HasMaxLength(4000);
            ticket.Property(t => t.CustomerName).HasMaxLength(200).IsRequired();
            ticket.Property(t => t.CreatedByUserId).HasMaxLength(450).IsRequired();
            ticket.Property(t => t.CreatedByName).HasMaxLength(200).IsRequired();
            ticket.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);

            // FK to the SHARED kernel Customer (cross-schema: support.Tickets → kernel.Customers).
            ticket.HasOne<Customer>().WithMany().HasForeignKey(t => t.CustomerId).OnDelete(DeleteBehavior.Restrict);

            ticket.HasMany(t => t.Messages).WithOne().HasForeignKey(m => m.TicketId).OnDelete(DeleteBehavior.Cascade);
            ticket.Navigation(t => t.Messages).UsePropertyAccessMode(PropertyAccessMode.Field);

            ticket.HasIndex(t => t.CustomerId);
            ticket.HasIndex(t => t.Status);
        });

        modelBuilder.Entity<TicketMessage>(message =>
        {
            message.ToTable("TicketMessages"); // child entity (not a DbSet)
            message.Property(m => m.AuthorUserId).HasMaxLength(450).IsRequired();
            message.Property(m => m.AuthorName).HasMaxLength(200).IsRequired();
            message.Property(m => m.Body).HasMaxLength(4000).IsRequired();
        });
    }
}
