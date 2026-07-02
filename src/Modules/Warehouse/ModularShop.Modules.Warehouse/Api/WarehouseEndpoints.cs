using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using ModularShop.Modules.Warehouse.Application;
using ModularShop.SharedKernel.Web;

namespace ModularShop.Modules.Warehouse.Api;

/// <summary>
/// The Warehouse module maps its OWN HTTP endpoints (minimal APIs). The host never defines these —
/// it just calls <c>MapEndpoints</c> on the module. This keeps a feature's code together.
/// </summary>
internal static class WarehouseEndpoints
{
    public static void MapWarehouseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/products");

        group.MapGet("/", async (ProductService service, CancellationToken ct)
            => (await service.GetProductsAsync(ct)).ToHttpResult());

        group.MapGet("/{id:guid}", async (Guid id, ProductService service, CancellationToken ct)
            => (await service.GetProductAsync(id, ct)).ToHttpResult());
    }
}
