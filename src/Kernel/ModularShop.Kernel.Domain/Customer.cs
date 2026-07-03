namespace ModularShop.Kernel.Domain;

/// <summary>
/// A customer of the shop. This is a <b>shared kernel entity</b>: several otherwise-unrelated modules
/// reference the same customer — Sales places orders for one, Shipping delivers to one, Support raises
/// tickets for one. Centralising it in the kernel (instead of each module keeping its own copy) is what
/// keeps a customer's identity consistent across the whole system. Modules link to it by <see cref="Entity.Id"/>.
/// </summary>
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
