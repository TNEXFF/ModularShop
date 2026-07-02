namespace ModularShop.SharedKernel.Infrastructure;

/// <summary>
/// Lets a module migrate its own schema and seed its own data at startup, without the host
/// knowing anything about the module's internals. The composition root simply resolves every
/// registered initializer and runs it. This keeps schema ownership inside each module.
/// </summary>
public interface IModuleInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
