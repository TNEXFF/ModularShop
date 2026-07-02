using Microsoft.AspNetCore.Mvc;
using ModularShop.Kernel.Web;
using ModularShop.Modules.Sales.Application;

namespace ModularShop.Modules.Sales.Api.Controllers;

[Route("api/customers")]
public sealed class CustomersController : ApiControllerBase
{
    private readonly ListCustomers _listCustomers;

    public CustomersController(ListCustomers listCustomers) => _listCustomers = listCustomers;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerDto>>>> List(CancellationToken ct)
        => ToApiResponse(await _listCustomers.ExecuteAsync(ct));
}
