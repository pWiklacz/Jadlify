using Jadlify.Domain.Planning;
using Jadlify.Domain.Recipes;

namespace Jadlify.Domain.Shopping;

public static class ShoppingListCalculator
{
    /// <summary>
    /// Aggregates planned meals into per-product gram totals. A recipe entry contributes each
    /// ingredient scaled by the planned share of the recipe; a product entry contributes its
    /// own grams directly. Both fold into the same <c>ProductId</c> bucket, so a product
    /// planned both ways appears once. Pairs for product entries carry a <c>null</c> recipe.
    /// </summary>
    public static IReadOnlyList<ShoppingListItem> ForMealEntries(
        IEnumerable<(MealPlanEntry Entry, Recipe? Recipe)> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        Dictionary<Guid, ShoppingListItemAccumulator> itemsByProductId = [];

        foreach ((MealPlanEntry entry, Recipe? recipe) in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);

            if (entry.Source is MealPlanEntrySource.Product)
            {
                Accumulate(itemsByProductId, entry.Product!.ProductId, entry.Product.Name, entry.Quantity);
                continue;
            }

            ArgumentNullException.ThrowIfNull(recipe);

            decimal portionFactor = entry.Quantity / recipe.Portions;

            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                Accumulate(
                    itemsByProductId,
                    ingredient.ProductId,
                    ingredient.ProductName,
                    ingredient.WholeRecipeAmount.Value * portionFactor);
            }
        }

        return itemsByProductId.Values
            .Select(item => item.ToShoppingListItem())
            .OrderBy(item => item.ProductName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ProductId)
            .ToList();
    }

    private static void Accumulate(
        Dictionary<Guid, ShoppingListItemAccumulator> itemsByProductId,
        Guid productId,
        string productName,
        decimal grams)
    {
        if (itemsByProductId.TryGetValue(productId, out ShoppingListItemAccumulator? existing))
        {
            existing.Add(grams);
            return;
        }

        itemsByProductId.Add(productId, new ShoppingListItemAccumulator(productId, productName, grams));
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
