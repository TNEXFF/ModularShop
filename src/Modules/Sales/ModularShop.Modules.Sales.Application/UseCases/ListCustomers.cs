using Ardalis.Result;
using Ardalis.Specification;
using ModularShop.Modules.Sales.Domain;

namespace ModularShop.Modules.Sales.Application;

/// <summary>Use case: list customers (ordered by name).</summary>
public sealed class ListCustomers
{
    private readonly IReadRepositoryBase<Customer> _customers;

    public ListCustomers(IReadRepositoryBase<Customer> customers) => _customers = customers;

    public async Task<Result<IReadOnlyList<CustomerDto>>> ExecuteAsync(CancellationToken ct)
    {
        var customers = await _customers.ListAsync(new CustomersOrderedSpec(), ct);
        return Result<IReadOnlyList<CustomerDto>>.Success(customers.Select(c => c.ToDto()).ToList());
    }
}
