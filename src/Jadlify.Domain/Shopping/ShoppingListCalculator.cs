using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Products;
using Jadlify.Domain.Recipes;

namespace Jadlify.Domain.Shopping;

public static class ShoppingListCalculator
{
    /// <summary>
    /// Decimal places grams are rounded to when they leave the calculator for storage or
    /// comparison. Fixing it here removes provider divergence — Postgres rounds a
    /// <c>numeric(12,3)</c> column while SQLite stores the decimal verbatim — so a stored list
    /// and a freshly computed projection compare like-for-like regardless of the database.
    /// </summary>
    public const int GramScale = 3;

    /// <summary>Rounds grams to the canonical scale used for stored items, contributions, and diffs.</summary>
    public static decimal RoundGrams(decimal grams) =>
        Math.Round(grams, GramScale, MidpointRounding.ToEven);

    /// <summary>
    /// Aggregates planned meals into per-product gram totals with their source contributions. A
    /// recipe entry contributes each ingredient scaled by the planned share of the recipe, its
    /// source labelled by the recipe name; a product entry contributes its own grams directly,
    /// labelled by the product name. Both fold into the same <c>ProductId</c> bucket, so a
    /// product planned both ways appears once with both contributions. Pairs for product entries
    /// carry a <c>null</c> recipe.
    /// <para>
    /// Totals are returned unrounded — the shopping aggregate applies <see cref="RoundGrams"/>
    /// when it stores them, keeping the calculator a pure sum with no rounding of intermediates.
    /// </para>
    /// </summary>
    public static IReadOnlyList<ShoppingProjectionItem> ForMealEntries(
        IEnumerable<(MealPlanEntry Entry, Recipe? Recipe)> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        Dictionary<Guid, ShoppingItemAccumulator> itemsByProductId = [];

        foreach ((MealPlanEntry entry, Recipe? recipe) in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);

            if (entry.Source is MealPlanEntrySource.Product)
            {
                PlannedProductSnapshot product = entry.Product!;
                Accumulate(
                    itemsByProductId,
                    product.ProductId,
                    product.Name,
                    product.Category,
                    entry.Date,
                    entry.MealType,
                    product.Name,
                    entry.Quantity);
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
                    // Recipe ingredients carry no category; a category is only known when the
                    // same product is also planned directly, which the accumulator folds in.
                    category: null,
                    entry.Date,
                    entry.MealType,
                    recipe.Name,
                    ingredient.WholeRecipeAmount.Value * portionFactor);
            }
        }

        return itemsByProductId.Values
            .Select(item => item.ToProjectionItem())
            .OrderBy(item => item.ProductName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ProductId)
            .ToList();
    }

    /// <summary>
    /// Computes a stable fingerprint of a projection from its canonically sorted contributions,
    /// not just its totals. Two plans that produce the same product totals through different
    /// meals hash differently, so a source-only change is still detected. Grams are rounded to
    /// the canonical scale first, so the fingerprint of a stored list matches the fingerprint
    /// recomputed from an unchanged plan.
    /// </summary>
    public static string ComputeFingerprint(IReadOnlyList<ShoppingProjectionItem> projection)
    {
        ArgumentNullException.ThrowIfNull(projection);

        StringBuilder canonical = new();
        foreach (ShoppingProjectionItem item in projection.OrderBy(item => item.ProductId))
        {
            canonical
                .Append(item.ProductId.ToString("D"))
                .Append('|')
                .Append(Format(RoundGrams(item.Grams)))
                .Append('\n');

            foreach (ShoppingProjectionContribution contribution in SortContributions(item.Contributions))
            {
                canonical
                    .Append('\t')
                    .Append(contribution.Date.ToString("O", CultureInfo.InvariantCulture))
                    .Append('|')
                    .Append(contribution.MealType)
                    .Append('|')
                    .Append(contribution.SourceLabel)
                    .Append('|')
                    .Append(Format(RoundGrams(contribution.Grams)))
                    .Append('\n');
            }
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()));
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>Orders contributions deterministically for storage, fingerprinting, and diffing.</summary>
    public static IEnumerable<ShoppingProjectionContribution> SortContributions(
        IEnumerable<ShoppingProjectionContribution> contributions)
    {
        ArgumentNullException.ThrowIfNull(contributions);

        return contributions
            .OrderBy(contribution => contribution.Date)
            .ThenBy(contribution => contribution.MealType)
            .ThenBy(contribution => contribution.SourceLabel, StringComparer.Ordinal)
            .ThenBy(contribution => contribution.Grams);
    }

    private static string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    private static void Accumulate(
        Dictionary<Guid, ShoppingItemAccumulator> itemsByProductId,
        Guid productId,
        string productName,
        ProductCategory? category,
        DateOnly date,
        MealType mealType,
        string sourceLabel,
        decimal grams)
    {
        if (!itemsByProductId.TryGetValue(productId, out ShoppingItemAccumulator? existing))
        {
            existing = new ShoppingItemAccumulator(productId, productName);
            itemsByProductId.Add(productId, existing);
        }

        existing.Add(category, date, mealType, sourceLabel, grams);
    }

    private sealed class ShoppingItemAccumulator
    {
        private readonly Dictionary<(DateOnly Date, MealType MealType, string SourceLabel), decimal> _contributions = [];

        public ShoppingItemAccumulator(Guid productId, string productName)
        {
            ProductId = productId;
            ProductName = productName;
        }

        private Guid ProductId { get; }

        private string ProductName { get; }

        private ProductCategory? Category { get; set; }

        private decimal Grams { get; set; }

        public void Add(ProductCategory? category, DateOnly date, MealType mealType, string sourceLabel, decimal grams)
        {
            // First non-null category wins: a product planned only through recipes stays
            // uncategorised, but the same product planned directly contributes its snapshot
            // category for grouping without disturbing the earlier recipe contributions.
            Category ??= category;
            Grams += grams;

            (DateOnly Date, MealType MealType, string SourceLabel) key = (date, mealType, sourceLabel);
            _contributions[key] = _contributions.GetValueOrDefault(key) + grams;
        }

        public ShoppingProjectionItem ToProjectionItem()
        {
            List<ShoppingProjectionContribution> contributions =
            [
                .. _contributions.Select(pair => new ShoppingProjectionContribution(
                    pair.Key.Date,
                    pair.Key.MealType,
                    pair.Key.SourceLabel,
                    pair.Value))
            ];

            return new ShoppingProjectionItem(ProductId, ProductName, Category, Grams, contributions);
        }
    }
}
