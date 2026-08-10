using Jadlify.Domain.Planning;
using Jadlify.Domain.Products;

namespace Jadlify.Domain.Shopping;

/// <summary>
/// One aggregated shopping line as computed straight from a plan: a product, the total grams
/// needed across the selected days, and the individual meal contributions that make up that
/// total. This is the calculator's output — a pure projection with no identity or persisted
/// state. A stored <see cref="ShoppingListItem"/> is built from it, and a refresh diffs the
/// stored list against a freshly computed set of these.
/// </summary>
public sealed record ShoppingProjectionItem(
    Guid ProductId,
    string ProductName,
    ProductCategory? Category,
    decimal Grams,
    IReadOnlyList<ShoppingProjectionContribution> Contributions);

/// <summary>
/// One meal's contribution to a product's total: which day and meal it came from, a human
/// label for that source (a recipe name or the product's own name), and the grams it added.
/// Contributions are what make the "by meal" / "by day" views and the <c>SourceFingerprint</c>
/// possible — a change in composition that leaves the total unchanged is still a source change.
/// </summary>
public sealed record ShoppingProjectionContribution(
    DateOnly Date,
    MealType MealType,
    string SourceLabel,
    decimal Grams);
