using Ardalis.Specification;
using ModularShop.Modules.Shipping.Domain;

namespace ModularShop.Modules.Shipping.Application;

public sealed class ShipmentsWithItemsSpec : Specification<Shipment>
{
    public ShipmentsWithItemsSpec()
    {
        Query.Include(s => s.Items);
        Query.OrderByDescending(s => s.CreatedOnUtc);
        Query.AsNoTracking();
    }
}

/// <summary>Read-only single shipment (AsNoTracking) — used by the GET endpoints.</summary>
public sealed class ShipmentByIdSpec : Specification<Shipment>
{
    public ShipmentByIdSpec(Guid id)
    {
        Query.Where(s => s.Id == id);
        Query.Include(s => s.Items);
        Query.AsNoTracking();
    }
}

/// <summary>Tracked single shipment — used when advancing its state (ship / deliver), so changes persist.</summary>
public sealed class ShipmentByIdForUpdateSpec : Specification<Shipment>
{
    public ShipmentByIdForUpdateSpec(Guid id)
    {
        Query.Where(s => s.Id == id);
        Query.Include(s => s.Items);
    }
}
