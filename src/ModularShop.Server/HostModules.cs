using ModularShop.Kernel.Infrastructure;
using ModularShop.Kernel.Web;
using ModularShop.Modules.Sales.Infrastructure;
using ModularShop.Modules.Shipping.Infrastructure;
using ModularShop.Modules.Support.Infrastructure;
using ModularShop.Modules.Warehouse.Infrastructure;

namespace ModularShop.Server;

/// <summary>
/// The one place that knows the full set of modules. Both the runtime (<c>Program.cs</c>) and the
/// design-time migration factory build from this list, so they can never drift apart. Each module —
/// <b>including the kernel</b>, which is just a special module — implements <see cref="IModule"/>: it
/// registers its own services and declares the context it contributes to the single host model.
/// </summary>
internal static class HostModules
{
    // Kernel FIRST: it sets the default "kernel" schema and owns Identity + the shared entities (Customer,
    // Currency) that the feature modules create cross-schema foreign keys to.
    public static IReadOnlyList<IModule> All() =>
    [
        new KernelModule(),
        new SalesModule(),
        new WarehouseModule(),
        new ShippingModule(),
        new SupportModule(),
    ];
}
