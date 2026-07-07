using Microsoft.AspNetCore.Mvc;
using ModularShop.Kernel.Api;
using ModularShop.Modules.Sales.Application.Dtos;
using ModularShop.Modules.Sales.Application.UseCases;

namespace ModularShop.Modules.Sales.Api.Controllers;

[Route("api/customers")]
public sealed class CustomersController : ApiControllerBase
{
    private readonly ListCustomersUseCase _listCustomers;

    public CustomersController(ListCustomersUseCase listCustomers) => _listCustomers = listCustomers;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerDto>>>> List(CancellationToken ct)
        => ToApiResponse(await _listCustomers.ExecuteAsync(ct));
}
