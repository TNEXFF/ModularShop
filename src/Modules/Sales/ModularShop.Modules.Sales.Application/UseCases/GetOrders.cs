using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using ModularShop.Modules.Sales.Domain;

namespace ModularShop.Modules.Sales.Application;

/// <summary>
/// Use case: list all orders (most recent first), each with its lines. Services depend on the shared
/// <see cref="DbContext"/> (resolved to the single host context) and query with plain LINQ —
/// there are no repositories or specification classes.
/// </summary>
public sealed class GetOrders
{
    private readonly DbContext _db;

    public GetOrders(DbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<OrderDto>>> ExecuteAsync(CancellationToken ct)
    {
        var orders = await _db.Set<Order>()
            .Include(o => o.Lines)
            .OrderByDescending(o => o.PlacedOnUtc)
            .AsNoTracking()
            .ToListAsync(ct);

        return Result<IReadOnlyList<OrderDto>>.Success(orders.Select(o => o.ToDto()).ToList());
    }
}
