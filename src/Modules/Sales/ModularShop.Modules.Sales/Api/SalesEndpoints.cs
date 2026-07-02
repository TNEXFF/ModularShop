using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using ModularShop.Modules.Sales.Application;
using ModularShop.SharedKernel.Web;

namespace ModularShop.Modules.Sales.Api;

internal static class SalesEndpoints
{
    public static void MapSalesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var orders = endpoints.MapGroup("/api/orders");
        orders.MapGet("/", async (OrderService service, CancellationToken ct)
            => (await service.GetOrdersAsync(ct)).ToHttpResult());
        orders.MapGet("/{id:guid}", async (Guid id, OrderService service, CancellationToken ct)
            => (await service.GetOrderAsync(id, ct)).ToHttpResult());
        orders.MapPost("/", async (PlaceOrderRequest request, OrderService service, CancellationToken ct)
            => (await service.PlaceOrderAsync(request, ct)).ToHttpResult());

        var customers = endpoints.MapGroup("/api/customers");
        customers.MapGet("/", async (CustomerService service, CancellationToken ct)
            => (await service.GetCustomersAsync(ct)).ToHttpResult());
    }
}
