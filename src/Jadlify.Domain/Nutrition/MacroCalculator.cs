using Jadlify.Domain.Planning;
using Jadlify.Domain.Products;
using Jadlify.Domain.Recipes;

namespace Jadlify.Domain.Nutrition;

public static class MacroCalculator
{
    private const decimal NutrientBasisGrams = 100m;

    public static MacroNutrients ForProductAmount(Product product, GramAmount amount)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(amount);

        return product.Per100Grams.Scale(amount.Value / NutrientBasisGrams);
    }

    public static MacroNutrients RecipeTotal(Recipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        MacroNutrients total = MacroNutrients.Zero;

        foreach (RecipeIngredient ingredient in recipe.Ingredients)
        {
            total += ingredient.Per100Grams.Scale(ingredient.WholeRecipeAmount.Value / NutrientBasisGrams);
        }

        return total;
    }

    public static MacroNutrients RecipePerServing(Recipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        return RecipeTotal(recipe).Scale(1m / recipe.Portions);
    }

    /// <summary>
    /// Macros for one planned meal. A recipe entry scales the recipe's per-serving values by
    /// its portion count and needs <paramref name="recipe"/>; a product entry scales its own
    /// snapshot by grams and ignores <paramref name="recipe"/>. Every step stays in decimal —
    /// nothing is rounded here, so callers see full precision.
    /// </summary>
    public static MacroNutrients ForMealEntry(MealPlanEntry entry, Recipe? recipe)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.Source is MealPlanEntrySource.Product)
        {
            return entry.Product!.Per100Grams.Scale(entry.Quantity / NutrientBasisGrams);
        }

        ArgumentNullException.ThrowIfNull(recipe);

        return RecipePerServing(recipe).Scale(entry.Quantity);
    }

    /// <summary>
    /// Sums a day's planned meals. Each pair carries the entry and, for a recipe entry, the
    /// recipe it resolved to; a product entry pairs with <c>null</c> because its snapshot is
    /// self-contained. Callers drop entries whose recipe could not be resolved before calling.
    /// </summary>
    public static MacroNutrients DayTotal(IEnumerable<(MealPlanEntry Entry, Recipe? Recipe)> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        MacroNutrients total = MacroNutrients.Zero;

        foreach ((MealPlanEntry entry, Recipe? recipe) in entries)
        {
            total += ForMealEntry(entry, recipe);
        }

        return total;
    }
}
