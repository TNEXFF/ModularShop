using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using ModularShop.Kernel.Domain;

namespace ModularShop.Modules.Sales.Application;

/// <summary>
/// Use case: list customers (ordered by name). <see cref="Customer"/> is a shared kernel entity, so
/// Sales reads it straight from the host context — the customer list is consistent for every module.
/// </summary>
public sealed class ListCustomers
{
    private readonly DbContext _db;

    public ListCustomers(DbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<CustomerDto>>> ExecuteAsync(CancellationToken ct)
    {
        var customers = await _db.Set<Customer>()
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync(ct);

        return Result<IReadOnlyList<CustomerDto>>.Success(customers.Select(c => c.ToDto()).ToList());
    }
}
