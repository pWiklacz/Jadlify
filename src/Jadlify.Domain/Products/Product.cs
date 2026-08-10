using Jadlify.Domain.Nutrition;

namespace Jadlify.Domain.Products;

public sealed class Product
{
    private Product()
    {
        // EF Core materialization constructor; populated through mapped members.
        Name = null!;
        Per100Grams = null!;
        Details = null!;
    }

    public Product(
        Guid id,
        string name,
        MacroNutrients per100Grams,
        string? barcode = null,
        decimal? packageSizeGrams = null,
        NutritionFacts? details = null,
        string? brand = null,
        ProductCategory? category = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(per100Grams);
        if (packageSizeGrams is { } size)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        }

        Id = id;
        Name = name;
        Per100Grams = per100Grams;
        Barcode = barcode;
        PackageSizeGrams = packageSizeGrams;
        Details = details ?? NutritionFacts.Empty;
        Brand = brand;
        Category = category;
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public string? Barcode { get; private set; }

    /// <summary>Optional manufacturer/brand, free text; <c>null</c> when unknown.</summary>
    public string? Brand { get; private set; }

    /// <summary>Optional taxonomy bucket; <c>null</c> is a valid "Bez kategorii" state.</summary>
    public ProductCategory? Category { get; private set; }

    public MacroNutrients Per100Grams { get; private set; }

    /// <summary>Net package size in grams, when known — enables per-package macro math.</summary>
    public decimal? PackageSizeGrams { get; private set; }

    /// <summary>Extended per-100g nutrition profile; <see cref="NutritionFacts.Empty"/> when none recorded.</summary>
    public NutritionFacts Details { get; private set; }
}
