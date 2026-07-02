using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using ModularShop.Modules.Shipping.Application;
using ModularShop.SharedKernel.Web;

namespace ModularShop.Modules.Shipping.Api;

internal static class ShippingEndpoints
{
    public static void MapShippingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/shipments");

        group.MapGet("/", async (ShipmentService service, CancellationToken ct)
            => (await service.GetShipmentsAsync(ct)).ToHttpResult());

        group.MapGet("/{id:guid}", async (Guid id, ShipmentService service, CancellationToken ct)
            => (await service.GetShipmentAsync(id, ct)).ToHttpResult());

        // Simple state-advancing actions for the demo (Pending -> Shipped -> Delivered).
        group.MapPost("/{id:guid}/ship", async (Guid id, ShipmentService service, CancellationToken ct)
            => (await service.ShipAsync(id, ct)).ToHttpResult());

        group.MapPost("/{id:guid}/deliver", async (Guid id, ShipmentService service, CancellationToken ct)
            => (await service.DeliverAsync(id, ct)).ToHttpResult());
    }
}
