using Ardalis.Specification;
using ModularShop.Modules.Sales.Domain;

namespace ModularShop.Modules.Sales.Application;

/// <summary>Ardalis.Specification queries for the Sales module (read-only projections use AsNoTracking).</summary>
public sealed class OrdersWithLinesSpec : Specification<Order>
{
    public OrdersWithLinesSpec()
    {
        Query.Include(o => o.Lines);
        Query.OrderByDescending(o => o.PlacedOnUtc);
        Query.AsNoTracking();
    }
}

public sealed class OrderByIdSpec : Specification<Order>
{
    public OrderByIdSpec(Guid id)
    {
        Query.Where(o => o.Id == id);
        Query.Include(o => o.Lines);
        Query.AsNoTracking();
    }
}

public sealed class CustomerByIdSpec : Specification<Customer>
{
    public CustomerByIdSpec(Guid id)
    {
        Query.Where(c => c.Id == id);
        Query.AsNoTracking();
    }
}

public sealed class CustomersOrderedSpec : Specification<Customer>
{
    public CustomersOrderedSpec()
    {
        Query.OrderBy(c => c.Name);
        Query.AsNoTracking();
    }
}
