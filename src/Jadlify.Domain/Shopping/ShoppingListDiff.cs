using Jadlify.Domain.Products;

namespace Jadlify.Domain.Shopping;

/// <summary>
/// One line of a refresh diff. <see cref="PreviousGrams"/> is <c>null</c> for an added product
/// and <see cref="NewGrams"/> is <c>null</c> for a removed one; both are present when a line
/// changed or only its sources moved.
/// </summary>
public sealed record ShoppingListDiffLine(
    Guid ProductId,
    string ProductName,
    ProductCategory? Category,
    decimal? PreviousGrams,
    decimal? NewGrams);

/// <summary>
/// The difference between a stored shopping list and a freshly computed projection of its
/// source days. The four sections drive both the preview modal and how bought state is carried
/// across a refresh: <see cref="Added"/> and <see cref="Changed"/> lines are (or become)
/// unbought because the shopper faces a new or different amount, while <see cref="SourceOnly"/>
/// lines keep their tick because the total they need is unchanged. Unchanged products are not
/// surfaced. Comparison and every stored/projected gram amount use the calculator's canonical
/// rounding, so the diff is provider-independent.
/// </summary>
public sealed record ShoppingListDiff(
    IReadOnlyList<ShoppingListDiffLine> Added,
    IReadOnlyList<ShoppingListDiffLine> Removed,
    IReadOnlyList<ShoppingListDiffLine> Changed,
    IReadOnlyList<ShoppingListDiffLine> SourceOnly)
{
    /// <summary>True when applying the refresh would alter the list in any observable way.</summary>
    public bool HasChanges => Added.Count > 0 || Removed.Count > 0 || Changed.Count > 0 || SourceOnly.Count > 0;

    /// <summary>
    /// Compares the stored <paramref name="items"/> against a freshly computed
    /// <paramref name="projection"/>, keyed by product. A product in both with the same rounded
    /// total is Unchanged if its source composition also matches, otherwise SourceOnly; a
    /// different total is Changed; a product on only one side is Added or Removed.
    /// </summary>
    public static ShoppingListDiff Between(
        IReadOnlyList<ShoppingListItem> items,
        IReadOnlyList<ShoppingProjectionItem> projection)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(projection);

        var itemsByProduct = items.ToDictionary(item => item.ProductId);
        var projectionByProduct = projection.ToDictionary(item => item.ProductId);

        List<ShoppingListDiffLine> added = [];
        List<ShoppingListDiffLine> removed = [];
        List<ShoppingListDiffLine> changed = [];
        List<ShoppingListDiffLine> sourceOnly = [];

        foreach (ShoppingProjectionItem projected in projection)
        {
            decimal newGrams = ShoppingListCalculator.RoundGrams(projected.Grams);

            if (!itemsByProduct.TryGetValue(projected.ProductId, out ShoppingListItem? existing))
            {
                added.Add(new ShoppingListDiffLine(
                    projected.ProductId, projected.ProductName, projected.Category, null, newGrams));
                continue;
            }

            if (existing.Grams != newGrams)
            {
                changed.Add(new ShoppingListDiffLine(
                    projected.ProductId, projected.ProductName, projected.Category, existing.Grams, newGrams));
            }
            else if (!SourcesMatch(existing, projected))
            {
                sourceOnly.Add(new ShoppingListDiffLine(
                    projected.ProductId, projected.ProductName, projected.Category, existing.Grams, newGrams));
            }
        }

        foreach (ShoppingListItem existing in items)
        {
            if (!projectionByProduct.ContainsKey(existing.ProductId))
            {
                removed.Add(new ShoppingListDiffLine(
                    existing.ProductId, existing.ProductName, existing.Category, existing.Grams, null));
            }
        }

        return new ShoppingListDiff(
            Order(added), Order(removed), Order(changed), Order(sourceOnly));
    }

    private static IReadOnlyList<ShoppingListDiffLine> Order(List<ShoppingListDiffLine> lines) =>
        [.. lines
            .OrderBy(line => line.ProductName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(line => line.ProductId)];

    private static bool SourcesMatch(ShoppingListItem existing, ShoppingProjectionItem projected)
    {
        (DateOnly, Planning.MealType, string, decimal)[] existingSignature =
        [
            .. existing.Sources.Select(source =>
                (source.Date, source.MealType, source.SourceLabel, source.Grams))
        ];

        (DateOnly, Planning.MealType, string, decimal)[] projectedSignature =
        [
            .. ShoppingListCalculator.SortContributions(projected.Contributions)
                .Select(contribution =>
                    (contribution.Date,
                     contribution.MealType,
                     contribution.SourceLabel,
                     ShoppingListCalculator.RoundGrams(contribution.Grams)))
        ];

        return existingSignature.AsSpan().SequenceEqual(projectedSignature);
    }
}
