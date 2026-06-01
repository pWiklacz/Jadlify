using Jadlify.Domain.Nutrition;

namespace Jadlify.Domain.Recipes;

public sealed class RecipeIngredient
{
    private RecipeIngredient()
    {
        // EF Core materialization constructor; populated through mapped members.
        ProductName = null!;
        Per100Grams = null!;
        WholeRecipeAmount = null!;
    }

    public RecipeIngredient(
        Guid productId,
        string productName,
        MacroNutrients per100Grams,
        GramAmount wholeRecipeAmount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        ArgumentNullException.ThrowIfNull(per100Grams);
        ArgumentNullException.ThrowIfNull(wholeRecipeAmount);

        ProductId = productId;
        ProductName = productName;
        Per100Grams = per100Grams;
        WholeRecipeAmount = wholeRecipeAmount;
    }

    public Guid ProductId { get; }

    public string ProductName { get; private set; }

    public MacroNutrients Per100Grams { get; private set; }

    /// <summary>
    /// Gram amount of this product used across the whole recipe, not per serving.
    /// </summary>
    public GramAmount WholeRecipeAmount { get; private set; }
}
