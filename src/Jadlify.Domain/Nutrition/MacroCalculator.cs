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

    public static MacroNutrients ForMealEntry(MealPlanEntry entry, Recipe recipe)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return RecipePerServing(recipe).Scale(entry.Portions);
    }
}
