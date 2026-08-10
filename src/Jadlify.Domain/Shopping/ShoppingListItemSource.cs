using Jadlify.Domain.Planning;

namespace Jadlify.Domain.Shopping;

/// <summary>
/// A frozen record of one meal's contribution to a shopping-list item: which day and meal it
/// came from, a label for that source (a recipe name or the product's own name), and the grams
/// it added. These back the "by meal" and "by day" views and the "used in" expander, and they
/// are a snapshot — later plan edits do not rewrite a stored contribution; a refresh replaces
/// the whole set.
/// </summary>
public sealed class ShoppingListItemSource
{
    private ShoppingListItemSource()
    {
        // EF Core materialization constructor.
        SourceLabel = null!;
    }

    public ShoppingListItemSource(DateOnly date, MealType mealType, string sourceLabel, decimal grams)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLabel);

        // A client-generated surrogate, fresh on every construction. A refresh rebuilds the whole
        // set, so new ids avoid any key collision between the replaced rows and their successors.
        Id = Guid.NewGuid();
        Date = date;
        MealType = mealType;
        SourceLabel = sourceLabel;
        Grams = grams;
    }

    public Guid Id { get; private set; }

    public DateOnly Date { get; private set; }

    public MealType MealType { get; private set; }

    public string SourceLabel { get; private set; }

    public decimal Grams { get; private set; }

    internal static ShoppingListItemSource FromContribution(ShoppingProjectionContribution contribution)
    {
        ArgumentNullException.ThrowIfNull(contribution);

        return new ShoppingListItemSource(
            contribution.Date,
            contribution.MealType,
            contribution.SourceLabel,
            ShoppingListCalculator.RoundGrams(contribution.Grams));
    }
}
