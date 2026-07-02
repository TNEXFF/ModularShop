using Ardalis.Specification;
using ModularShop.Modules.Warehouse.Domain;

namespace ModularShop.Modules.Warehouse.Application;

/// <summary>
/// Ardalis.Specification queries for the Warehouse module. Specifications live in the Application
/// layer and describe WHAT to query; the repository (Infrastructure) knows HOW to run them against
/// the DbContext. This keeps the Application layer free of any EF/DbContext dependency.
/// </summary>
public sealed class ProductsCatalogSpec : Specification<Product>
{
    public ProductsCatalogSpec()
    {
        Query.OrderBy(p => p.Category).ThenBy(p => p.Name);
        Query.AsNoTracking();
    }
}

public sealed class ProductByIdSpec : Specification<Product>
{
    public ProductByIdSpec(Guid id)
    {
        Query.Where(p => p.Id == id);
        Query.AsNoTracking();
    }
}

public sealed class ProductsByIdsSpec : Specification<Product>
{
    public ProductsByIdsSpec(IReadOnlyCollection<Guid> ids)
    {
        Query.Where(p => ids.Contains(p.Id));
        Query.AsNoTracking();
    }
}

/// <summary>Loads products for a stock update — tracked (no AsNoTracking), so changes persist.</summary>
public sealed class ProductsByIdsForUpdateSpec : Specification<Product>
{
    public ProductsByIdsForUpdateSpec(IReadOnlyCollection<Guid> ids)
        => Query.Where(p => ids.Contains(p.Id));
}
