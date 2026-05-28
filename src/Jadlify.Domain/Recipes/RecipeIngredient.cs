using Jadlify.Domain.Nutrition;

namespace Jadlify.Domain.Recipes;

public sealed class RecipeIngredient
{
    public RecipeIngredient(Guid productId, GramAmount wholeRecipeAmount)
    {
        ArgumentNullException.ThrowIfNull(wholeRecipeAmount);

        ProductId = productId;
        WholeRecipeAmount = wholeRecipeAmount;
    }

    public Guid ProductId { get; }

    /// <summary>
    /// Gram amount of this product used across the whole recipe, not per serving.
    /// </summary>
    public GramAmount WholeRecipeAmount { get; private set; }
}
