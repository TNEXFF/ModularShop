using ModularShop.Kernel.Domain;

namespace ModularShop.Modules.Sales.Domain;

/// <summary>
/// An order (aggregate root), owned by the Sales module. Its lines are added through
/// <see cref="AddLine"/>, which snapshots the product name and price so the order does not depend on
/// the Warehouse module keeping that product or price unchanged.
/// <para>
/// <see cref="CustomerId"/> is a foreign key to the <b>shared kernel</b> <see cref="Customer"/>, and
/// <see cref="CurrencyCode"/> a foreign key to the shared kernel <see cref="Currency"/> — both live in
/// the kernel so every module agrees on the same customers and currencies.
/// </para>
/// </summary>
public sealed class Order : Entity
{
    private readonly List<OrderLine> _lines = new();

    public string OrderNumber { get; private set; } = default!;
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = default!;
    public string CurrencyCode { get; private set; } = default!;
    public string PlacedBy { get; private set; } = default!;
    public DateTime PlacedOnUtc { get; private set; }
    public OrderStatus Status { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines;
    public decimal Total => _lines.Sum(l => l.LineTotal);

    private Order() { } // EF

    public Order(Guid id, string orderNumber, Guid customerId, string customerName, string placedBy,
        DateTime placedOnUtc, string currencyCode = "USD")
        : base(id)
    {
        OrderNumber = orderNumber;
        CustomerId = customerId;
        CustomerName = customerName;
        CurrencyCode = currencyCode;
        PlacedBy = placedBy;
        PlacedOnUtc = placedOnUtc;
        Status = OrderStatus.Placed;
    }

    public void AddLine(Guid productId, string productName, decimal unitPrice, int quantity)
        => _lines.Add(new OrderLine(Guid.NewGuid(), Id, productId, productName, unitPrice, quantity));

    /// <summary>Used only by seeding, to represent historical completed orders.</summary>
    public void MarkCompleted() => Status = OrderStatus.Completed;

    /// <summary>Used only by seeding, to represent a historical cancelled order.</summary>
    public void MarkCancelled() => Status = OrderStatus.Cancelled;
}
