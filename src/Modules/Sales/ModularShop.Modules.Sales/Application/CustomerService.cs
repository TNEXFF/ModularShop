using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Sales.Infrastructure;
using ModularShop.SharedKernel.Domain;

namespace ModularShop.Modules.Sales.Application;

internal sealed class CustomerService
{
    private readonly SalesDbContext _db;

    public CustomerService(SalesDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<CustomerDto>>> GetCustomersAsync(CancellationToken ct)
    {
        var customers = await _db.Customers.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CustomerDto(c.Id, c.Name, c.Email))
            .ToListAsync(ct);
        return Result<IReadOnlyList<CustomerDto>>.Success(customers);
    }
}
