using Ardalis.Result;
using ModularShop.Kernel.Domain.Repositories;
using ModularShop.Modules.Shipping.Application.Dtos;
using ModularShop.Modules.Shipping.Application.Mappings;
using ModularShop.Modules.Shipping.Domain;

namespace ModularShop.Modules.Shipping.Application.UseCases;

/// <summary>Use case: list all shipments (most recent first), each with its items.</summary>
public sealed class ListShipments
{
    private readonly IReadRepository<Shipment> _shipments;

    public ListShipments(IReadRepository<Shipment> shipments) => _shipments = shipments;

    public async Task<Result<IReadOnlyList<ShipmentDto>>> ExecuteAsync(CancellationToken ct)
    {
        var shipments = await _shipments.ListWithIncludesAsync(
            predicate: null,
            orderBy: q => q.OrderByDescending(s => s.CreatedOnUtc),
            cancellationToken: ct,
            s => s.Items);

        return Result<IReadOnlyList<ShipmentDto>>.Success(shipments.Select(s => s.ToDto()).ToList());
    }
}
