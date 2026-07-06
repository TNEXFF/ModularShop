using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Modules.Support.Application.Queries;
using ModularShop.Modules.Support.Application.UseCases;
using ModularShop.Modules.Support.Infrastructure.Persistence;

namespace ModularShop.Modules.Support.Infrastructure;

/// <summary>
/// The Support module's composition root (its <see cref="IModule"/>). Support is a <b>genuinely
/// unrelated</b> module: it publishes and consumes NO integration events (so it registers no MediatR) and
/// calls into no other module. It only uses the kernel (the shared <c>Customer</c> + Identity), which is
/// exactly the point — an independent module can coexist and still share the kernel's cross-cutting parts.
/// </summary>
public sealed class SupportModule : IModule
{
    public string Name => "Support";
    public Type ContextType => typeof(SupportDbContext);

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddUseCases(typeof(CreateTicketUseCase).Assembly);

        // Support's specific read query (NOT a repository — the generic IRepository<Ticket> is registered
        // by the kernel and covers all entity CRUD). Used by ListTicketsUseCase for its efficient
        // count-projection query, which a repository has no business returning.
        services.AddScoped<ITicketSummaryQuery, TicketSummaryQuery>();

        services.AddScoped<IModuleInitializer, SupportSeeder>();
    }
}
