using ModularShop.Modules.Shipping.Domain;

namespace ModularShop.Modules.Shipping.Application;

internal static class ShippingMappings
{
    public static ShipmentDto ToDto(this Shipment s) => new(
        s.Id, s.ShipmentNumber, s.OrderId, s.OrderNumber, s.CustomerName, s.Status.ToString(),
        s.CreatedOnUtc, s.ShippedOnUtc, s.DeliveredOnUtc, s.Carrier, s.TrackingNumber, s.TotalUnits,
        s.Items.Select(i => new ShipmentItemDto(i.ProductName, i.Quantity)).ToList());
}
