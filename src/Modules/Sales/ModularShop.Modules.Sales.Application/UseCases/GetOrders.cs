using Ardalis.Result;
using Ardalis.Specification;
using ModularShop.Modules.Sales.Domain;

namespace ModularShop.Modules.Sales.Application;

/// <summary>Use case: list all orders (most recent first), each with its lines.</summary>
public sealed class GetOrders
{
    private readonly IReadRepositoryBase<Order> _orders;

    public GetOrders(IReadRepositoryBase<Order> orders) => _orders = orders;

    public async Task<Result<IReadOnlyList<OrderDto>>> ExecuteAsync(CancellationToken ct)
    {
        var orders = await _orders.ListAsync(new OrdersWithLinesSpec(), ct);
        return Result<IReadOnlyList<OrderDto>>.Success(orders.Select(o => o.ToDto()).ToList());
    }
}
