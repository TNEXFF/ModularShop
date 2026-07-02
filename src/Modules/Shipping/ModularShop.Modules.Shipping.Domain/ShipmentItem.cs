using ModularShop.Kernel.Domain;

namespace ModularShop.Modules.Shipping.Domain;

public sealed class ShipmentItem : Entity
{
    public Guid ShipmentId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public int Quantity { get; private set; }

    private ShipmentItem() { } // EF

    public ShipmentItem(Guid id, Guid shipmentId, string productName, int quantity) : base(id)
    {
        ShipmentId = shipmentId;
        ProductName = productName;
        Quantity = quantity;
    }
}
