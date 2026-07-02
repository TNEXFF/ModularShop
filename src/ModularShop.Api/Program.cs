using Microsoft.Extensions.DependencyInjection;
using ModularShop.Modules.Sales;
using ModularShop.Modules.Shipping;
using ModularShop.Modules.Warehouse;
using ModularShop.SharedKernel.Infrastructure;
using ModularShop.SharedKernel.Web;

// ─────────────────────────────────────────────────────────────────────────────────────────────
//  ModularShop — the COMPOSITION ROOT (a.k.a. the module bootstrapper / host).
//
//  This is the only project that knows the full set of modules. It contains NO business logic —
//  it just wires modules together. Each module registers its own services + DbContext and maps
//  its own endpoints through the IModule contract. Adding a feature = add a module project and a
//  single line to the list below.
// ─────────────────────────────────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// The modules that make up this monolith, in load order.
IReadOnlyList<IModule> modules =
[
    new SalesModule(),
    new WarehouseModule(),
    new ShippingModule(),
];

// Cross-cutting services from the shared kernel.
builder.Services.AddSharedInfrastructure();   // the in-process integration-event bus
builder.Services.AddSharedWeb();              // current-user accessor, HttpContext access

// CORS so the React dev server (Vite on :5173) can call the API during development. In production
// the API serves the built SPA from wwwroot (same origin), so CORS is not needed there.
const string DevCorsPolicy = "dev-spa";
builder.Services.AddCors(options => options.AddPolicy(DevCorsPolicy, policy => policy
    .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

// Let each module register ITS OWN services and DbContext.
foreach (var module in modules)
    module.Register(builder.Services, builder.Configuration);

var app = builder.Build();

// On startup, let each module migrate its own schema and seed its own data. The host does not
// know how — it only resolves the IModuleInitializer implementations the modules registered.
using (var scope = app.Services.CreateScope())
{
    foreach (var initializer in scope.ServiceProvider.GetServices<IModuleInitializer>())
        await initializer.InitializeAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors(DevCorsPolicy);

// Serve the built React SPA as part of the SAME deployable unit (a core Modular Monolith trait).
// These are no-ops until the client is built into wwwroot.
app.UseDefaultFiles();
app.UseStaticFiles();

// Each module maps ITS OWN HTTP endpoints.
foreach (var module in modules)
    module.MapEndpoints(app);

// Small info endpoint that lists the loaded modules — handy for the demo.
app.MapGet("/api", () => Results.Ok(new
{
    application = "ModularShop",
    description = "A Modular Monolith teaching example (ASP.NET Core + React, MSSQL).",
    modules = modules.Select(m => m.Name).ToArray()
}));

// SPA fallback: non-API routes return index.html so client-side routing works.
app.MapFallbackToFile("index.html");

app.Run();
