using ModularShop.Modules.Warehouse.Domain;

namespace ModularShop.Modules.Warehouse.Application;

internal static class ProductMappings
{
    public static ProductResponse ToResponse(this Product p) =>
        new(p.Id, p.Sku, p.Name, p.Description, p.Category, p.Price, p.CurrencyCode, p.StockQuantity);
}
