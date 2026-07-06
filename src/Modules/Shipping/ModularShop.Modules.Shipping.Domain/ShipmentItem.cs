using ModularShop.Kernel.Domain;

namespace ModularShop.Modules.Shipping.Domain;

/// <summary><see cref="ShipmentId"/> is set by EF Core from the owning shipment's navigation when the graph is saved.</summary>
public sealed class ShipmentItem : Entity
{
    public Guid ShipmentId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public int Quantity { get; private set; }

    private ShipmentItem() { } // EF

    public ShipmentItem(string productName, int quantity)
    {
        ProductName = productName;
        Quantity = quantity;
    }
}
