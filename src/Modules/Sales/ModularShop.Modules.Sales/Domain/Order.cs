using ModularShop.SharedKernel.Domain;

namespace ModularShop.Modules.Sales.Domain;

/// <summary>
/// An order (aggregate root), owned by the Sales module. Its lines are added through
/// <see cref="AddLine"/>, which snapshots the product name and price so the order does not depend
/// on the Warehouse module keeping that product or price unchanged.
/// </summary>
internal sealed class Order : Entity
{
    private readonly List<OrderLine> _lines = new();

    public string OrderNumber { get; private set; } = default!;
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = default!;
    public string PlacedBy { get; private set; } = default!;
    public DateTime PlacedOnUtc { get; private set; }
    public OrderStatus Status { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines;
    public decimal Total => _lines.Sum(l => l.LineTotal);

    private Order() { } // EF

    public Order(Guid id, string orderNumber, Guid customerId, string customerName, string placedBy, DateTime placedOnUtc)
        : base(id)
    {
        OrderNumber = orderNumber;
        CustomerId = customerId;
        CustomerName = customerName;
        PlacedBy = placedBy;
        PlacedOnUtc = placedOnUtc;
        Status = OrderStatus.Placed;
    }

    public void AddLine(Guid productId, string productName, decimal unitPrice, int quantity)
        => _lines.Add(new OrderLine(Guid.NewGuid(), Id, productId, productName, unitPrice, quantity));

    /// <summary>Used only by seeding, to represent historical completed orders.</summary>
    public void MarkCompleted() => Status = OrderStatus.Completed;
}
