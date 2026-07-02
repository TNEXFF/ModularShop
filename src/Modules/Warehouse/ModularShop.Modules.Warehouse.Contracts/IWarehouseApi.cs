namespace ModularShop.Modules.Warehouse.Contracts;

/// <summary>
/// The Warehouse module's PUBLIC, synchronous API — the only way other modules may read Warehouse
/// data. It exposes use-case-shaped methods that return DTOs, never domain entities or the
/// DbContext. It is implemented internally inside the Warehouse module and supplied via DI, so
/// callers (e.g. Sales) depend on this interface and never see the implementation. This is the
/// SYNCHRONOUS inter-module communication style (used when the caller needs an answer now).
/// </summary>
public interface IWarehouseApi
{
    /// <summary>Returns current price and available stock for the given products.</summary>
    Task<IReadOnlyList<ProductStock>> GetProductsAsync(
        IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default);
}

/// <summary>A minimal, public projection of a product for other modules.</summary>
public sealed record ProductStock(Guid Id, string Sku, string Name, decimal Price, int StockAvailable);
