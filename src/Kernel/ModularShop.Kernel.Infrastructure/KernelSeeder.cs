using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModularShop.Kernel.Domain;
using ModularShop.Kernel.Infrastructure.Identity;

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

        _db.Set<Currency>().AddRange(
            new Currency("USD", "$", "US Dollar"),
            new Currency("EUR", "€", "Euro"),
            new Currency("GBP", "£", "Pound Sterling"));
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
        new(C(1),  "Alice Johnson", "alice.johnson@contoso.com"),
        new(C(2),  "Bob Martinez",  "bob.martinez@contoso.com"),
        new(C(3),  "Carla Nguyen",  "carla.nguyen@fabrikam.com"),
        new(C(4),  "David Smith",   "david.smith@fabrikam.com"),
        new(C(5),  "Emma Wilson",   "emma.wilson@northwind.com"),
        new(C(6),  "Farah Khan",    "farah.khan@northwind.com"),
        new(C(7),  "George Brown",  "george.brown@adventure-works.com"),
        new(C(8),  "Hana Suzuki",   "hana.suzuki@adventure-works.com"),
        new(C(9),  "Ivan Petrov",   "ivan.petrov@contoso.com"),
        new(C(10), "Julia Rossi",   "julia.rossi@fabrikam.com"),
    ];
}
