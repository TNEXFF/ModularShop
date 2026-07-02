using ModularShop.Kernel.Domain;

namespace ModularShop.Modules.Warehouse.Domain;

/// <summary>
/// A product in the catalogue, owned entirely by the Warehouse module. Its type is visible only
/// within the Warehouse module's own projects — no OTHER module references the Warehouse Domain
/// assembly, so the boundary is enforced by the project-reference graph. Other modules see only the
/// public <c>ProductStock</c> DTO returned by <c>IWarehouseApi</c>.
/// </summary>
public sealed class Product : Entity
{
    public string Sku { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string Category { get; private set; } = default!;
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }

    private Product() { } // EF

    public Product(Guid id, string sku, string name, string description, string category, decimal price, int stockQuantity)
        : base(id)
    {
        Sku = sku;
        Name = name;
        Description = description;
        Category = category;
        Price = price;
        StockQuantity = stockQuantity;
    }

    /// <summary>Reduces available stock (never below zero). Called when an order is placed.</summary>
    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0) return;
        StockQuantity = Math.Max(0, StockQuantity - quantity);
    }
}
