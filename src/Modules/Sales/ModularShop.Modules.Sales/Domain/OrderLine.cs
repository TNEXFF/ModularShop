using ModularShop.SharedKernel.Domain;

namespace ModularShop.Modules.Sales.Domain;

/// <summary>
/// A line on an order. Note <see cref="ProductId"/> is the id of a <b>Warehouse</b> product, but
/// there is deliberately NO foreign key to the Warehouse tables — that would cross a module
/// boundary. Instead the name and price are <b>snapshotted</b> at order time: the data Sales needs
/// is copied into Sales' own schema, so the modules stay independent.
/// </summary>
internal sealed class OrderLine : Entity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal => UnitPrice * Quantity;

    private OrderLine() { } // EF

    public OrderLine(Guid id, Guid orderId, Guid productId, string productName, decimal unitPrice, int quantity)
        : base(id)
    {
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}
