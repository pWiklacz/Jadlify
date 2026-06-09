using Jadlify.Domain.Planning;
using Jadlify.Domain.Recipes;

namespace Jadlify.Domain.Shopping;

public static class ShoppingListCalculator
{
    public static IReadOnlyList<ShoppingListItem> ForMealEntries(
        IEnumerable<(MealPlanEntry Entry, Recipe Recipe)> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        Dictionary<Guid, ShoppingListItemAccumulator> itemsByProductId = [];

        foreach ((MealPlanEntry entry, Recipe recipe) in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(recipe);

            decimal portionFactor = entry.Portions / (decimal)recipe.Portions;

            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                decimal scaledGrams = ingredient.WholeRecipeAmount.Value * portionFactor;

                if (itemsByProductId.TryGetValue(ingredient.ProductId, out ShoppingListItemAccumulator? existing))
                {
                    existing.Add(scaledGrams);
                    continue;
                }

                itemsByProductId.Add(
                    ingredient.ProductId,
                    new ShoppingListItemAccumulator(ingredient.ProductId, ingredient.ProductName, scaledGrams));
            }
        }

        return itemsByProductId.Values
            .Select(item => item.ToShoppingListItem())
            .OrderBy(item => item.ProductName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ProductId)
            .ToList();
    }

    private sealed class ShoppingListItemAccumulator
    {
        public ShoppingListItemAccumulator(Guid productId, string productName, decimal grams)
        {
            ProductId = productId;
            ProductName = productName;
            Grams = grams;
        }

        private Guid ProductId { get; }

        private string ProductName { get; }

        private decimal Grams { get; set; }

        public void Add(decimal grams) => Grams += grams;

        public ShoppingListItem ToShoppingListItem() => new(ProductId, ProductName, Grams);
    }
}
