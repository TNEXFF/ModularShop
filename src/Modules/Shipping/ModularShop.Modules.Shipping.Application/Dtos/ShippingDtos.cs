namespace ModularShop.Modules.Shipping.Application.Dtos;

public sealed record ShipmentItemDto(string ProductName, int Quantity);

public sealed record ShipmentDto(
    Guid Id,
    string ShipmentNumber,
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    string Status,
    DateTime CreatedOnUtc,
    DateTime? ShippedOnUtc,
    DateTime? DeliveredOnUtc,
    string? Carrier,
    string? TrackingNumber,
    int TotalUnits,
    IReadOnlyList<ShipmentItemDto> Items);
