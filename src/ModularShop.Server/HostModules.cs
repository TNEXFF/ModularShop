using ModularShop.Kernel.Infrastructure;
using ModularShop.Kernel.Infrastructure.Persistence;
using ModularShop.Modules.Sales.Infrastructure;
using ModularShop.Modules.Shipping.Infrastructure;
using ModularShop.Modules.Support.Infrastructure;
using ModularShop.Modules.Warehouse.Infrastructure;

namespace ModularShop.Server;

/// <summary>
/// The one place that knows the full set of modules. Both the runtime (<c>Program.cs</c>) and the
/// design-time migration factory build from this list, so they can never drift apart. Each
/// <c>XModule</c> implements BOTH <see cref="IModule"/> (its services) and <see cref="IModuleModel"/>
/// (its slice of the single host model), so one instance serves both roles.
/// </summary>
internal static class HostModules
{
    public static IReadOnlyList<IModule> All() =>
    [
        new SalesModule(),
        new WarehouseModule(),
        new ShippingModule(),
        new SupportModule(),
    ];

    /// <summary>The same modules, viewed as model contributors (used by the design-time factory).</summary>
    public static IReadOnlyList<IModuleModel> Models() => All().Cast<IModuleModel>().ToList();
}
