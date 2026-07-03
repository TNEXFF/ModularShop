using Ardalis.Result;
using ModularShop.Kernel.Domain.Repositories;
using ModularShop.Modules.Sales.Domain;

namespace ModularShop.Modules.Sales.Application;

/// <summary>Use case: fetch a single order (with its lines) by id.</summary>
public sealed class GetOrder
{
    private readonly IReadRepository<Order> _orders;

    public GetOrder(IReadRepository<Order> orders) => _orders = orders;

    public async Task<Result<OrderDto>> ExecuteAsync(Guid id, CancellationToken ct)
    {
        var order = await _orders.GetWithIncludesAsync(o => o.Id == id, ct, o => o.Lines);

        return order is null
            ? Result<OrderDto>.NotFound($"Order {id} was not found.")
            : Result<OrderDto>.Success(order.ToDto());
    }
}
