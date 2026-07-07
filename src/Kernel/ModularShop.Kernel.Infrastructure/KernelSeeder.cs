using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModularShop.Kernel.Domain;
using ModularShop.Kernel.Domain.Identity;

namespace ModularShop.Kernel.Infrastructure;

/// <summary>
/// Seeds the data the KERNEL owns and every module depends on: the currency list, the shared customer
/// list, and the Identity roles + demo users. It implements <see cref="IModuleInitializer"/> with a low
/// <see cref="IModuleInitializer.Order"/> so it runs <b>before</b> the module seeders — e.g. so a seeded
/// order can reference a customer that already exists.
/// </summary>
public sealed class KernelSeeder : IModuleInitializer
{
    /// <summary>Password for every seeded demo account (shown on the sign-in screen for convenience).</summary>
    public const string DemoPassword = "Passw0rd!";

    private readonly DbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<ApplicationRole> _roles;
    private readonly ILogger<KernelSeeder> _logger;

    public KernelSeeder(
        DbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles,
        ILogger<KernelSeeder> logger)
    {
        _db = db;
        _users = users;
        _roles = roles;
        _logger = logger;
    }

    public int Order => 0; // kernel data first; modules default to 100

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await SeedCurrenciesAsync(cancellationToken);
        await SeedCustomersAsync(cancellationToken);
        await SeedIdentityAsync();
    }

    private async Task SeedCurrenciesAsync(CancellationToken ct)
    {
        if (await _db.Set<Currency>().AnyAsync(ct)) return;

        // The demo prices everything in a single currency (USD) for simplicity. Currency is still a shared
        // kernel entity referenced by both Warehouse (Product) and Sales (Order), demonstrating a
        // cross-module lookup — there just happens to be one row.
        _db.Set<Currency>().Add(new Currency("USD", "$", "US Dollar"));
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded kernel currencies.");
    }

    private async Task SeedCustomersAsync(CancellationToken ct)
    {
        if (await _db.Set<Customer>().AnyAsync(ct)) return;

        _db.Set<Customer>().AddRange(Customers());
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded kernel customers ({Count}).", await _db.Set<Customer>().CountAsync(ct));
    }

    private async Task SeedIdentityAsync()
    {
        foreach (var role in new[] { Roles.Admin, Roles.Agent })
            if (!await _roles.RoleExistsAsync(role))
                await _roles.CreateAsync(new ApplicationRole(role));

        await EnsureUserAsync("admin@modularshop.local", "Ada Admin", Roles.Admin);
        await EnsureUserAsync("agent@modularshop.local", "Sam Agent", Roles.Agent);

        _logger.LogInformation("Seeded Identity roles and demo users (password '{Password}').", DemoPassword);
    }

    private async Task EnsureUserAsync(string email, string displayName, string role)
    {
        if (await _users.FindByEmailAsync(email) is not null) return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
        };
        var result = await _users.CreateAsync(user, DemoPassword);
        if (result.Succeeded)
            await _users.AddToRoleAsync(user, role);
        else
            _logger.LogWarning("Could not seed user {Email}: {Errors}", email,
                string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    /// <summary>Well-known roles used across the app.</summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Agent = "Agent";
    }

    // Customer ids use the fixed prefix 20000000-… so seeded orders / shipments / tickets in the modules
    // can reference them deterministically across runs.
    private static Guid C(int n) => new($"20000000-0000-0000-0000-0000000000{n:D2}");

    private static IReadOnlyList<Customer> Customers() =>
    [
        new("Alice Johnson", "alice.johnson@contoso.com",        id: C(1)),
        new("Bob Martinez",  "bob.martinez@contoso.com",         id: C(2)),
        new("Carla Nguyen",  "carla.nguyen@fabrikam.com",        id: C(3)),
        new("David Smith",   "david.smith@fabrikam.com",         id: C(4)),
        new("Emma Wilson",   "emma.wilson@northwind.com",        id: C(5)),
        new("Farah Khan",    "farah.khan@northwind.com",         id: C(6)),
        new("George Brown",  "george.brown@adventure-works.com", id: C(7)),
        new("Hana Suzuki",   "hana.suzuki@adventure-works.com",  id: C(8)),
        new("Ivan Petrov",   "ivan.petrov@contoso.com",          id: C(9)),
        new("Julia Rossi",   "julia.rossi@fabrikam.com",         id: C(10)),
    ];
}
