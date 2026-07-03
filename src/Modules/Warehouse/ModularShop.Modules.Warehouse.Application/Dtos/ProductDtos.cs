namespace ModularShop.Modules.Warehouse.Application;

/// <summary>Public shape of a product for the catalogue endpoints (this module's own API).</summary>
public sealed record ProductResponse(
    Guid Id, string Sku, string Name, string Description, string Category,
    decimal Price, string CurrencyCode, int StockQuantity);
