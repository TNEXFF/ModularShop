using ModularShop.Modules.Sales.Domain;

namespace ModularShop.Modules.Sales.Application;

internal static class SalesMappings
{
    public static OrderDto ToDto(this Order order) => new(
        order.Id,
        order.OrderNumber,
        order.CustomerId,
        order.CustomerName,
        order.Status.ToString(),
        order.PlacedOnUtc,
        order.PlacedBy,
        order.Total,
        order.Lines
            .Select(l => new OrderLineDto(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity, l.LineTotal))
            .ToList());

    public static CustomerDto ToDto(this Customer customer) =>
        new(customer.Id, customer.Name, customer.Email);
}
