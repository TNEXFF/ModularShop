using Ardalis.Result;
using ModularShop.Kernel.Domain.Repositories;
using ModularShop.Modules.Sales.Domain;

namespace ModularShop.Modules.Sales.Application;

/// <summary>
/// Use case: list all orders (most recent first), each with its lines. It depends on the generic
/// <see cref="IReadRepository{T}"/> — never on EF Core or the DbContext — and lets the repository run the
/// ordering and the eager-load in the database.
/// </summary>
public sealed class GetOrders
{
    private readonly IReadRepository<Order> _orders;

    public GetOrders(IReadRepository<Order> orders) => _orders = orders;

    public async Task<Result<IReadOnlyList<OrderDto>>> ExecuteAsync(CancellationToken ct)
    {
        var orders = await _orders.ListWithIncludesAsync(
            predicate: null,
            orderBy: q => q.OrderByDescending(o => o.PlacedOnUtc),
            cancellationToken: ct,
            o => o.Lines);

        return Result<IReadOnlyList<OrderDto>>.Success(orders.Select(o => o.ToDto()).ToList());
    }
}
