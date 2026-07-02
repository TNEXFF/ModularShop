using Ardalis.Result;
using Ardalis.Specification;
using ModularShop.Modules.Shipping.Domain;

namespace ModularShop.Modules.Shipping.Application;

/// <summary>Use case: list all shipments (most recent first), each with its items.</summary>
public sealed class ListShipments
{
    private readonly IReadRepositoryBase<Shipment> _shipments;

    public ListShipments(IReadRepositoryBase<Shipment> shipments) => _shipments = shipments;

    public async Task<Result<IReadOnlyList<ShipmentDto>>> ExecuteAsync(CancellationToken ct)
    {
        var shipments = await _shipments.ListAsync(new ShipmentsWithItemsSpec(), ct);
        return Result<IReadOnlyList<ShipmentDto>>.Success(shipments.Select(s => s.ToDto()).ToList());
    }
}
