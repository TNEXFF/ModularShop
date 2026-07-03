namespace ModularShop.Kernel.Domain;

/// <summary>
/// A currency, keyed by its ISO-4217 code (e.g. <c>"USD"</c>). A second <b>shared kernel entity</b>:
/// Warehouse prices its products in a currency and Sales totals its orders in one, so both modules must
/// agree on a single, consistent list of currencies. Small reference/look-up data like this is a natural
/// fit for the kernel. It is keyed by <see cref="Code"/> rather than a surrogate id, so foreign keys read
/// naturally (e.g. a product's currency is literally <c>"USD"</c>).
/// </summary>
public sealed class Currency
{
    public string Code { get; private set; } = default!;   // ISO-4217, primary key (e.g. "USD")
    public string Symbol { get; private set; } = default!; // e.g. "$"
    public string Name { get; private set; } = default!;   // e.g. "US Dollar"

    private Currency() { } // EF

    public Currency(string code, string symbol, string name)
    {
        Code = code;
        Symbol = symbol;
        Name = name;
    }
}
