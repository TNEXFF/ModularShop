using ModularShop.Kernel.Domain;
using ModularShop.Modules.Sales.Application.Dtos;
using ModularShop.Modules.Sales.Domain;

namespace ModularShop.Modules.Sales.Application.Mappings;

internal static class SalesMappings
{
    public static OrderDto ToDto(this Order order) => new(
        order.Id,
        order.OrderNumber,
        order.CustomerId,
        order.CustomerName,
        order.CurrencyCode,
        order.Status.ToString(),
        order.PlacedOnUtc,
        order.PlacedBy,
        order.Total,
        order.Lines
            .Select(l => new OrderLineDto(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity, l.LineTotal))
            .ToList());

    // Customer is a shared kernel entity; Sales reads it (it is allowed to depend on the kernel) but
    // never owns or writes it.
    public static CustomerDto ToDto(this Customer customer) =>
        new(customer.Id, customer.Name, customer.Email);
}
