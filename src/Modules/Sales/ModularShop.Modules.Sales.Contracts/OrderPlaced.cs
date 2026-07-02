using MediatR;

namespace ModularShop.Modules.Sales.Contracts;

/// <summary>
/// Integration event published by the Sales module after an order has been committed. It is a
/// MediatR <see cref="INotification"/>, so any module can subscribe with an
/// <c>INotificationHandler&lt;OrderPlaced&gt;</c>. It is part of the Sales module's PUBLIC contract,
/// so it is intentionally small and stable. Other modules (Warehouse, Shipping) learn about orders
/// ONLY through this event, never by reading the Sales tables. This is the ASYNCHRONOUS inter-module
/// communication style.
/// </summary>
public sealed record OrderPlaced(
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    IReadOnlyList<OrderPlacedLine> Lines) : INotification;

/// <summary>One line of a placed order, carrying just what subscribers need.</summary>
public sealed record OrderPlacedLine(Guid ProductId, string ProductName, int Quantity);
