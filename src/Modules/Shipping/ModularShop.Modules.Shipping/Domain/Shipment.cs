using ModularShop.SharedKernel.Domain;

namespace ModularShop.Modules.Shipping.Domain;

/// <summary>
/// A shipment, owned by the Shipping module. A shipment is created from an <c>OrderPlaced</c>
/// integration event — Shipping only ever learns about an order through that event, never by
/// reading the Sales tables. It keeps just the order info it needs (a copy), plus its own state.
/// </summary>
internal sealed class Shipment : Entity
{
    private readonly List<ShipmentItem> _items = new();

    public string ShipmentNumber { get; private set; } = default!;
    public Guid OrderId { get; private set; }
    public string OrderNumber { get; private set; } = default!;
    public string CustomerName { get; private set; } = default!;
    public ShipmentStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? ShippedOnUtc { get; private set; }
    public DateTime? DeliveredOnUtc { get; private set; }
    public string? Carrier { get; private set; }
    public string? TrackingNumber { get; private set; }

    public IReadOnlyCollection<ShipmentItem> Items => _items;
    public int TotalUnits => _items.Sum(i => i.Quantity);

    private Shipment() { } // EF

    public Shipment(Guid id, string shipmentNumber, Guid orderId, string orderNumber, string customerName, DateTime createdOnUtc)
        : base(id)
    {
        ShipmentNumber = shipmentNumber;
        OrderId = orderId;
        OrderNumber = orderNumber;
        CustomerName = customerName;
        CreatedOnUtc = createdOnUtc;
        Status = ShipmentStatus.Pending;
    }

    public void AddItem(string productName, int quantity) => _items.Add(new ShipmentItem(Guid.NewGuid(), Id, productName, quantity));

    public bool Ship(string carrier, string trackingNumber)
    {
        if (Status != ShipmentStatus.Pending) return false;
        Status = ShipmentStatus.Shipped;
        Carrier = carrier;
        TrackingNumber = trackingNumber;
        ShippedOnUtc = DateTime.UtcNow;
        return true;
    }

    public bool Deliver()
    {
        if (Status != ShipmentStatus.Shipped) return false;
        Status = ShipmentStatus.Delivered;
        DeliveredOnUtc = DateTime.UtcNow;
        return true;
    }

    /// <summary>Used only by seeding, to place a historical shipment into a known past state.</summary>
    internal void ApplySeedState(ShipmentStatus status, string? carrier, string? trackingNumber, DateTime? shippedOnUtc, DateTime? deliveredOnUtc)
    {
        Status = status;
        Carrier = carrier;
        TrackingNumber = trackingNumber;
        ShippedOnUtc = shippedOnUtc;
        DeliveredOnUtc = deliveredOnUtc;
    }
}
