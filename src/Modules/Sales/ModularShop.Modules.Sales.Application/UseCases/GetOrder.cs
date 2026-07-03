using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Sales.Domain;

namespace ModularShop.Modules.Sales.Application;

/// <summary>Use case: fetch a single order (with its lines) by id.</summary>
public sealed class GetOrder
{
    private readonly DbContext _db;

    public GetOrder(DbContext db) => _db = db;

    public async Task<Result<OrderDto>> ExecuteAsync(Guid id, CancellationToken ct)
    {
        var order = await _db.Set<Order>()
            .Include(o => o.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        return order is null
            ? Result<OrderDto>.NotFound($"Order {id} was not found.")
            : Result<OrderDto>.Success(order.ToDto());
    }
}
