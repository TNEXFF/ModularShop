using MediatR;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Kernel.Web;
using ModularShop.Modules.Sales.Api;
using ModularShop.Modules.Sales.Infrastructure;
using ModularShop.Modules.Shipping.Api;
using ModularShop.Modules.Shipping.Infrastructure;
using ModularShop.Modules.Warehouse.Api;
using ModularShop.Modules.Warehouse.Infrastructure;

// ─────────────────────────────────────────────────────────────────────────────────────────────
//  ModularShop.Server — the COMPOSITION ROOT (a.k.a. the module bootstrapper / host).
//
//  This is the only project that knows the full set of modules. It contains NO business logic — it
//  just wires modules together. Each module registers its own services + DbContext through the
//  IModule contract; its controllers live in the module's Api project and are registered here as
//  MVC ApplicationParts. Adding a feature = add a module and a single line to the list below.
// ─────────────────────────────────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// The modules that make up this monolith, in load order.
IReadOnlyList<IModule> modules =
[
    new SalesModule(),
    new WarehouseModule(),
    new ShippingModule(),
];

// MediatR is the in-process integration-event bus (it replaces the previous hand-rolled bus). It
// scans each module's Infrastructure assembly for INotificationHandler<> implementations — that is
// how Warehouse and Shipping subscribe to the Sales OrderPlaced event.
builder.Services.AddMediatR(cfg =>
{
    // MediatR's Community licence is free for education / small orgs. A key is optional (without one
    // MediatR only logs a notice); set MediatR:LicenseKey in configuration to silence it.
    var licenseKey = builder.Configuration["MediatR:LicenseKey"];
    if (!string.IsNullOrWhiteSpace(licenseKey))
        cfg.LicenseKey = licenseKey;

    cfg.RegisterServicesFromAssemblies(modules.Select(m => m.GetType().Assembly).ToArray());
});

// Kernel cross-cutting web services (the current-user accessor).
builder.Services.AddKernelWeb();

// Controllers live in each module's Api project; register those assemblies as MVC ApplicationParts
// so the host discovers their controllers.
builder.Services.AddControllers()
    .AddApplicationPart(typeof(SalesApiAssembly).Assembly)
    .AddApplicationPart(typeof(WarehouseApiAssembly).Assembly)
    .AddApplicationPart(typeof(ShippingApiAssembly).Assembly);

// Swagger / OpenAPI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS so the React dev server (Vite on :5173) can call the API during development. In production the
// host serves the built SPA from wwwroot (same origin), so CORS is not needed there.
const string DevCorsPolicy = "dev-spa";
builder.Services.AddCors(options => options.AddPolicy(DevCorsPolicy, policy => policy
    .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

// Register the module list itself (so the AppInfo diagnostic endpoint can enumerate it) and let each
// module register ITS OWN services and DbContext.
foreach (var module in modules)
{
    builder.Services.AddSingleton(module);
    module.Register(builder.Services, builder.Configuration);
}

var app = builder.Build();

// On startup, let each module migrate its own schema and seed its own data. The host does not know
// how — it only resolves the IModuleInitializer implementations the modules registered.
using (var scope = app.Services.CreateScope())
{
    foreach (var initializer in scope.ServiceProvider.GetServices<IModuleInitializer>())
        await initializer.InitializeAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger is enabled in every environment so the API is always explorable at /swagger for this demo.
// (A production system would typically expose it only in non-production environments.)
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(DevCorsPolicy);

// Serve the built React SPA as part of the SAME deployable unit (a core Modular Monolith trait).
// These are no-ops until the client is built into wwwroot.
app.UseDefaultFiles();
app.UseStaticFiles();

// Module controllers (discovered via the ApplicationParts registered above).
app.MapControllers();

// SPA fallback: non-API routes return index.html so client-side routing works.
app.MapFallbackToFile("index.html");

app.Run();
