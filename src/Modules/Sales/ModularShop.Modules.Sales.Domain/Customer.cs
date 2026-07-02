using ModularShop.Kernel.Domain;

namespace ModularShop.Modules.Sales.Domain;

/// <summary>A customer, owned by the Sales module. Visible only inside the Sales module's projects.</summary>
public sealed class Customer : Entity
{
    public string Name { get; private set; } = default!;
    public string Email { get; private set; } = default!;

    private Customer() { } // EF

    public Customer(Guid id, string name, string email) : base(id)
    {
        Name = name;
        Email = email;
    }
}
