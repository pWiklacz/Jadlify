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

    public static MacroNutrients RecipeTotal(Recipe recipe, IReadOnlyDictionary<Guid, Product> productsById)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(productsById);

        MacroNutrients total = MacroNutrients.Zero;

        foreach (RecipeIngredient ingredient in recipe.Ingredients)
        {
            if (!productsById.TryGetValue(ingredient.ProductId, out Product? product))
            {
                throw new InvalidOperationException(
                    $"Product {ingredient.ProductId} required by recipe {recipe.Id} was not provided.");
            }

            total += ForProductAmount(product, ingredient.WholeRecipeAmount);
        }

        return total;
    }

    public static MacroNutrients RecipePerServing(Recipe recipe, IReadOnlyDictionary<Guid, Product> productsById)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        return RecipeTotal(recipe, productsById).Scale(1m / recipe.Portions);
    }

    public static MacroNutrients ForMealEntry(
        MealPlanEntry entry,
        Recipe recipe,
        IReadOnlyDictionary<Guid, Product> productsById)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return RecipePerServing(recipe, productsById).Scale(entry.Portions);
    }
}
