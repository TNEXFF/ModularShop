using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularShop.Kernel.Application;
using ModularShop.Kernel.Application.Abstractions;
using ModularShop.Kernel.Domain.Repositories;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Kernel.Infrastructure.Identity;
using ModularShop.Kernel.Infrastructure.Persistence;
using ModularShop.Kernel.Infrastructure.Persistence.Repositories;

namespace ModularShop.Kernel.Web;

/// <summary>
/// The kernel as a module. It is special — it owns the cross-cutting building blocks every other module
/// relies on — but it registers its own parts through the same <see cref="IModule"/> contract as any
/// feature module, and contributes its <see cref="KernelDbContext"/> to the one host model the same way.
/// It registers the generic repositories + unit of work, ASP.NET Core Identity (cookie auth, Guid keys)
/// over the host context, the current-user accessor, and the kernel seeder. Its controllers (the
/// <c>AuthController</c> in this assembly) are discovered by MVC exactly like every module's.
/// </summary>
public sealed class KernelModule : IModule
{
    public string Name => "Kernel";
    public Type ContextType => typeof(KernelDbContext);

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        // ── Data access: one open-generic repository + unit of work serve every module's entities ──────
        services.AddScoped(typeof(IReadRepository<>), typeof(Repository<>));
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── ASP.NET Core Identity with cookie auth. The stores target the host context, which is
        //    registered as the base DbContext — so the kernel needs no reference to the host type. ──────
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                // Relaxed password policy for the demo (the seeded accounts use "Passw0rd!").
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireDigit = false;
            })
            .AddEntityFrameworkStores<DbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "ModularShop.Auth";
            options.Cookie.HttpOnly = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;
            // This is an API: return status codes instead of redirecting to a login/access-denied page.
            options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
            options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
        });

        // ── Kernel web cross-cutting: the current-user accessor read from the authenticated principal ──
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        // ── Kernel data seeder (currencies, customers, roles, users). Order = 0 → before module seeders ─
        services.AddScoped<IModuleInitializer, KernelSeeder>();
    }
}
