using Jadlify.Domain.Nutrition;

namespace Jadlify.Domain.Products;

public sealed class Product
{
    public Product(Guid id, string name, MacroNutrients per100Grams, string? barcode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(per100Grams);

        Id = id;
        Name = name;
        Per100Grams = per100Grams;
        Barcode = barcode;
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public string? Barcode { get; private set; }

    public MacroNutrients Per100Grams { get; private set; }
}
