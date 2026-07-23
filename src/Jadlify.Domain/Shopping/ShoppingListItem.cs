using Jadlify.Domain.Products;

namespace Jadlify.Domain.Shopping;

/// <summary>
/// One line of a shopping list: a product, the grams needed across the list's source days, its
/// bought state, and the meal contributions that make up the total. Items are grouped by
/// <see cref="ProductId"/> — a product used by several meals is one line — and carry a
/// snapshot of the product's name and category so the line survives the catalog product being
/// renamed, recategorised, or deleted (no foreign key), exactly as recipe ingredients do.
/// </summary>
public sealed class ShoppingListItem
{
    private readonly List<ShoppingListItemSource> _sources = [];

    private ShoppingListItem()
    {
        // EF Core materialization constructor.
        ProductName = null!;
    }

    private ShoppingListItem(
        Guid id,
        Guid productId,
        string productName,
        ProductCategory? category,
        decimal grams)
    {
        Id = id;
        ProductId = productId;
        ProductName = productName;
        Category = category;
        Grams = grams;
        IsBought = false;
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; }

    /// <summary>Category snapshot for grouping; <c>null</c> ("Bez kategorii") is a valid state.</summary>
    public ProductCategory? Category { get; private set; }

    public decimal Grams { get; private set; }

    public bool IsBought { get; private set; }

    public IReadOnlyList<ShoppingListItemSource> Sources => _sources;

    /// <summary>Builds a fresh, unbought item from a projected line, giving it a new identity.</summary>
    public static ShoppingListItem FromProjection(ShoppingProjectionItem projection)
    {
        ArgumentNullException.ThrowIfNull(projection);

        ShoppingListItem item = new(
            Guid.NewGuid(),
            projection.ProductId,
            projection.ProductName,
            projection.Category,
            ShoppingListCalculator.RoundGrams(projection.Grams));
        item.ReplaceSources(projection.Contributions);
        return item;
    }

    /// <summary>Ticks the item bought or unbought.</summary>
    public void SetBought(bool isBought) => IsBought = isBought;

    /// <summary>
    /// Rewrites the item from a newly projected line during a refresh. <paramref name="resetBought"/>
    /// is <see langword="true"/> when the grams changed (the shopper needs a different amount, so
    /// a prior tick no longer holds) and <see langword="false"/> when only the source composition
    /// changed while the total stayed the same (the tick still holds). The name and category
    /// snapshots are refreshed too, so a stored line tracks the latest planned product data.
    /// </summary>
    public void ApplyProjection(ShoppingProjectionItem projection, bool resetBought)
    {
        ArgumentNullException.ThrowIfNull(projection);

        ProductName = projection.ProductName;
        Category = projection.Category;
        Grams = ShoppingListCalculator.RoundGrams(projection.Grams);
        ReplaceSources(projection.Contributions);

        if (resetBought)
        {
            IsBought = false;
        }
    }

    private void ReplaceSources(IReadOnlyList<ShoppingProjectionContribution> contributions)
    {
        _sources.Clear();
        foreach (ShoppingProjectionContribution contribution in
            ShoppingListCalculator.SortContributions(contributions))
        {
            _sources.Add(ShoppingListItemSource.FromContribution(contribution));
        }
    }
}
