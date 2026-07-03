namespace ModularShop.Kernel.Infrastructure;

/// <summary>
/// Lets a module (or the kernel) seed its own data at startup. With <b>centralised migrations</b> the
/// host migrates the single database once; each initializer then only seeds its own tables through the
/// shared host context. <see cref="Order"/> controls sequencing so that, e.g., the kernel's customers
/// exist before the Sales module seeds orders that reference them.
/// </summary>
public interface IModuleInitializer
{
    /// <summary>Lower runs earlier. The kernel seeder uses 0; modules default to 100.</summary>
    int Order => 100;

    Task InitializeAsync(CancellationToken cancellationToken = default);
}
