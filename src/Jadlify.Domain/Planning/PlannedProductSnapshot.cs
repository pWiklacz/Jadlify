using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Products;

namespace Jadlify.Domain.Planning;

/// <summary>
/// The product data a direct product entry was planned with, copied at plan time.
/// Follows the same "keep historical" policy as recipe ingredients: the catalog product
/// may later be renamed, re-categorized, re-valued, or deleted without changing what a
/// past day contained. <see cref="ProductId"/> is kept for grouping (shopping lists
/// aggregate by it) but is deliberately not a foreign key.
/// </summary>
public sealed class PlannedProductSnapshot
{
    private PlannedProductSnapshot()
    {
        // EF Core materialization constructor; populated through mapped members.
        Name = null!;
        Per100Grams = null!;
    }

    public PlannedProductSnapshot(
        Guid productId,
        string name,
        MacroNutrients per100Grams,
        ProductCategory? category = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(per100Grams);

        ProductId = productId;
        Name = name;
        Per100Grams = per100Grams;
        Category = category;
    }

    public Guid ProductId { get; }

    public string Name { get; private set; }

    /// <summary>Category as it stood when planned; <c>null</c> is the valid "Bez kategorii" state.</summary>
    public ProductCategory? Category { get; private set; }

    public MacroNutrients Per100Grams { get; private set; }

    /// <summary>Copies the current catalog state of <paramref name="product"/> into a snapshot.</summary>
    public static PlannedProductSnapshot FromProduct(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        return new PlannedProductSnapshot(product.Id, product.Name, product.Per100Grams, product.Category);
    }

    /// <summary>
    /// Returns an independent snapshot carrying the same planned values. Copying an entry must
    /// not hand the same snapshot instance to two entries: this is an owned type, so a shared
    /// instance would belong to two owners at once and the persistence layer would reject it.
    /// The macro values are rebuilt for the same reason.
    /// </summary>
    public PlannedProductSnapshot Copy() =>
        new(
            ProductId,
            Name,
            new MacroNutrients(
                Per100Grams.Calories,
                Per100Grams.Protein,
                Per100Grams.Fat,
                Per100Grams.Carbohydrates),
            Category);
}
