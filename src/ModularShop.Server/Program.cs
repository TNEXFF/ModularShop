using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ModularShop.Kernel.Application.Abstractions;
using ModularShop.Kernel.Domain.Repositories;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Kernel.Infrastructure.Identity;
using ModularShop.Kernel.Infrastructure.Persistence;
using ModularShop.Kernel.Infrastructure.Persistence.Repositories;
using ModularShop.Kernel.Web;
using ModularShop.Modules.Sales.Api;
using ModularShop.Modules.Shipping.Api;
using ModularShop.Modules.Support.Api;
using ModularShop.Modules.Warehouse.Api;
using ModularShop.Server;
using ModularShop.Server.Persistence;

// ─────────────────────────────────────────────────────────────────────────────────────────────
//  ModularShop.Server — the COMPOSITION ROOT (a.k.a. the host).
//
//  This is the only project that knows the full set of modules. It owns the SINGLE DbContext (which
//  every module's model is layered onto), the centralised migrations, and Identity — and it contains
//  no business logic. Each module registers its own services through IModule and contributes its slice
//  of the model through IModuleModel. Adding a feature = add a module and a single line to HostModules.
// ─────────────────────────────────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

var modules = HostModules.All();
var connectionString = builder.Configuration.GetConnectionString("ModularShopDemo");

// ── The single host context: owns the database + the centralised migrations ────────────────────
builder.Services.AddDbContext<ModularShopDbContext>(options => options.UseSqlServer(connectionString));

// Every repository depends on the base DbContext; alias it to the one host context.
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<ModularShopDbContext>());

// ── Data access: the generic repositories + unit of work, over the one host context ─────────────
// A single open-generic Repository<T> serves every module's entities (Option B has one context).
// Use cases depend on IReadRepository<T> / IRepository<T> (in the Domain) and IUnitOfWork (in the
// Application) — never on EF Core. A module registers its OWN specific repository only where the
// generic one falls short (see Support's ITicketRepository).
builder.Services.AddScoped(typeof(IReadRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ── ASP.NET Core Identity (a kernel concern) with cookie auth, stored in the host context ───────
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        // Relaxed password policy for the demo (the seeded accounts use "Passw0rd!").
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
    })
    .AddEntityFrameworkStores<ModularShopDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "ModularShop.Auth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    // This is an API: return status codes instead of redirecting to a login/access-denied page.
    options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
});

// ── MediatR: the in-process integration-event bus. Scans each module's Infrastructure assembly for
//    INotificationHandler<> implementations (how Warehouse & Shipping subscribe to OrderPlaced). ─────
builder.Services.AddMediatR(cfg =>
{
    var licenseKey = builder.Configuration["MediatR:LicenseKey"];
    if (!string.IsNullOrWhiteSpace(licenseKey))
        cfg.LicenseKey = licenseKey;

    cfg.RegisterServicesFromAssemblies(modules.Select(m => m.GetType().Assembly).ToArray());
});

// Kernel cross-cutting web services (the current-user accessor).
builder.Services.AddKernelWeb();

// Controllers live in each module's Api project (+ the kernel's AuthController); register the assemblies
// as MVC ApplicationParts so the host discovers their controllers.
builder.Services.AddControllers()
    .AddApplicationPart(typeof(AuthController).Assembly)          // kernel Web (authentication endpoints)
    .AddApplicationPart(typeof(SalesApiAssembly).Assembly)
    .AddApplicationPart(typeof(WarehouseApiAssembly).Assembly)
    .AddApplicationPart(typeof(ShippingApiAssembly).Assembly)
    .AddApplicationPart(typeof(SupportApiAssembly).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS so the React dev server (Vite on :5173) can call the API with cookies during development. In
// production the host serves the built SPA from wwwroot (same origin), so CORS is not needed there.
const string DevCorsPolicy = "dev-spa";
builder.Services.AddCors(options => options.AddPolicy(DevCorsPolicy, policy => policy
    .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials())); // required for the auth cookie over the dev proxy

// Register each module BOTH as IModule (diagnostics) and IModuleModel (contributes to the host model),
// then let it register its own services (use cases, public APIs, seeders).
foreach (var module in modules)
{
    builder.Services.AddSingleton<IModule>(module);
    builder.Services.AddSingleton<IModuleModel>((IModuleModel)module);
    module.Register(builder.Services, builder.Configuration);
}

// The kernel's own seeder (currencies, customers, roles, users). Order = 0, so it runs before modules.
builder.Services.AddScoped<IModuleInitializer, KernelSeeder>();

var app = builder.Build();

// ── Startup: migrate the single database ONCE, then run every seeder in Order ───────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ModularShopDbContext>();
    await db.Database.MigrateAsync();

    foreach (var initializer in scope.ServiceProvider.GetServices<IModuleInitializer>().OrderBy(i => i.Order))
        await initializer.InitializeAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger is enabled in every environment so the API is always explorable at /swagger for this demo.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(DevCorsPolicy);

// Serve the built React SPA as part of the SAME deployable unit (a core Modular Monolith trait). These
// are no-ops until the client is built into wwwroot, and are placed before auth so SPA assets load freely.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Module controllers (discovered via the ApplicationParts registered above).
app.MapControllers();

// SPA fallback: non-API routes return index.html so client-side routing works.
app.MapFallbackToFile("index.html");

app.Run();
