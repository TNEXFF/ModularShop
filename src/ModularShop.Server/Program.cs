using Microsoft.EntityFrameworkCore;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Kernel.Web;
using ModularShop.Server;
using ModularShop.Server.Persistence;

// ─────────────────────────────────────────────────────────────────────────────────────────────
//  ModularShop.Server — the COMPOSITION ROOT (a.k.a. the host).
//
//  This is the only project that knows the full set of modules. It owns the SINGLE DbContext (onto which
//  every module's model — the kernel's included — is layered), the centralised migrations, and the HTTP
//  pipeline. It contains NO module logic of its own: each module (the kernel is just a special one)
//  registers ALL of its own parts through IModule.Register — services, use cases, controllers, event bus
//  and seeders. Adding a feature = add a module and a single line to HostModules.
// ─────────────────────────────────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

var modules = HostModules.All();
var connectionString = builder.Configuration.GetConnectionString("ModularShopDemo");

// ── The single host context: owns the database + the centralised migrations (history kept in dbo) ──────
builder.Services.AddDbContext<ModularShopDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo")));

// Every service (the generic repositories AND the Identity stores) depends on the base DbContext; alias it
// to the one host context so a module never needs to reference the host's concrete type.
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<ModularShopDbContext>());

// ── Register each module (the kernel included) and let it register ALL of its own parts ────────────────
// The host adds nothing module-specific here — no repositories, no Identity, no MediatR, no controller
// lists. Each module owns those. Controllers ship inside the module assemblies and MVC discovers them.
foreach (var module in modules)
{
    builder.Services.AddSingleton<IModule>(module);
    module.Register(builder.Services, builder.Configuration);
}

// ── Host-level web composition: MVC, Swagger, CORS ─────────────────────────────────────────────────────
builder.Services.AddControllers();
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

var app = builder.Build();

// ── Startup: migrate the single database ONCE, then run every seeder in Order ──────────────────────────
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

// Module + kernel controllers (discovered by MVC from the referenced module assemblies).
app.MapControllers();

// SPA fallback: non-API routes return index.html so client-side routing works.
app.MapFallbackToFile("index.html");

app.Run();
