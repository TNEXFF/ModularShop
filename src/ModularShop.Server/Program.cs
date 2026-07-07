using Microsoft.EntityFrameworkCore;
using ModularShop.Infrastructure.Persistence;
using ModularShop.Kernel.Api;
using ModularShop.Kernel.Infrastructure;

// ─────────────────────────────────────────────────────────────────────────────────────────────
//  ModularShop.Server — the COMPOSITION ROOT (a.k.a. the host).
//
//  This is the web host: it wires the HTTP pipeline and the single DbContext, then hands off. Persistence
//  itself — the host context that layers every module's model onto one database, plus the centralised
//  migrations — lives in ModularShop.Infrastructure. The host contains NO module logic of its own: each
//  module (the kernel is just a special one) registers ALL of its own parts through IModule.Register —
//  services, use cases, controllers, event bus and seeders. Adding a feature = create a module and
//  reference it (optionally name it under "Modules"). Discovery, selection, model composition and the
//  migrate-then-seed startup routine all live behind AddModules / InitializeModulesAsync.
// ─────────────────────────────────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ModularShopDemo");

// ── The single host context (defined in ModularShop.Infrastructure): owns the database + the centralised
//    migrations (history kept in dbo). The host just registers it and points it at the connection. ──────
builder.Services.AddDbContext<ModularShopDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo")));

// Every service (the generic repositories AND the Identity stores) depends on the base DbContext; alias it
// to the one host context so a module never needs to reference the host's concrete type.
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<ModularShopDbContext>());

// ── Discover, select and register every module (the kernel included) ────────────────────────────────────
// AddModules scans the app's ModularShop.* assemblies for IModule implementations, keeps the ones named in
// the "Modules" configuration array (absent ⇒ all; the foundational kernel is always kept), and lets each
// register ALL of its own parts. The host adds nothing module-specific here. Controllers ship inside the
// module assemblies and MVC discovers them.
builder.Services.AddModules(builder.Configuration);

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

// ── Startup: migrate the one database, then run every module's seeder in Order (see ModuleRegistration) ─
await app.Services.InitializeModulesAsync();

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
