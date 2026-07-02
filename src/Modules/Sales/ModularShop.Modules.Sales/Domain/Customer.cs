using ModularShop.SharedKernel.Domain;

namespace ModularShop.Modules.Sales.Domain;

/// <summary>A customer, owned by the Sales module. <c>internal</c> — invisible to other modules.</summary>
internal sealed class Customer : Entity
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
