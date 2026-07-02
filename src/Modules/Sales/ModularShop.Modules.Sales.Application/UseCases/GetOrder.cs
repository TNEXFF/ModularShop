using Ardalis.Result;
using Ardalis.Specification;
using ModularShop.Modules.Sales.Domain;

namespace ModularShop.Modules.Sales.Application;

/// <summary>Use case: fetch a single order (with its lines) by id.</summary>
public sealed class GetOrder
{
    private readonly IReadRepositoryBase<Order> _orders;

    public GetOrder(IReadRepositoryBase<Order> orders) => _orders = orders;

    public async Task<Result<OrderDto>> ExecuteAsync(Guid id, CancellationToken ct)
    {
        var order = await _orders.FirstOrDefaultAsync(new OrderByIdSpec(id), ct);
        return order is null
            ? Result<OrderDto>.NotFound($"Order {id} was not found.")
            : Result<OrderDto>.Success(order.ToDto());
    }
}
