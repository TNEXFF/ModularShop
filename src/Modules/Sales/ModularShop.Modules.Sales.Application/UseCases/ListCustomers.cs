using Ardalis.Result;
using ModularShop.Kernel.Domain;
using ModularShop.Kernel.Domain.Repositories;
using ModularShop.Modules.Sales.Application.Dtos;
using ModularShop.Modules.Sales.Application.Mappings;

namespace ModularShop.Modules.Sales.Application.UseCases;

/// <summary>
/// Use case: list customers (ordered by name). <see cref="Customer"/> is a shared kernel entity, so Sales
/// reads it through the generic repository — the customer list is consistent for every module.
/// </summary>
public sealed class ListCustomers
{
    private readonly IReadRepository<Customer> _customers;

    public ListCustomers(IReadRepository<Customer> customers) => _customers = customers;

    public async Task<Result<IReadOnlyList<CustomerDto>>> ExecuteAsync(CancellationToken ct)
    {
        var customers = await _customers.ListAsync(
            orderBy: q => q.OrderBy(c => c.Name),
            cancellationToken: ct);

        return Result<IReadOnlyList<CustomerDto>>.Success(customers.Select(c => c.ToDto()).ToList());
    }
}
