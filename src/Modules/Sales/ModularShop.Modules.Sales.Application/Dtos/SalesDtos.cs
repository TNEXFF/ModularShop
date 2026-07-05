namespace ModularShop.Modules.Sales.Application.Dtos;

public sealed record CustomerDto(Guid Id, string Name, string Email);

public sealed record OrderLineDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    string CurrencyCode,
    string Status,
    DateTime PlacedOnUtc,
    string PlacedBy,
    decimal Total,
    IReadOnlyList<OrderLineDto> Lines);

/// <summary>Request body for placing an order.</summary>
public sealed record PlaceOrderRequest(Guid CustomerId, List<PlaceOrderLineRequest> Lines);

public sealed record PlaceOrderLineRequest(Guid ProductId, int Quantity);
