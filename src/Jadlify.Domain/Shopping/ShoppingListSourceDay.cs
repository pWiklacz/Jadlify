namespace Jadlify.Domain.Shopping;

/// <summary>
/// One day a shopping list draws its items from. The set of these is the arbitrary, bounded
/// selection the user made when creating the list; a refresh recomputes the items from exactly
/// these days, so they are stored rather than re-derived.
/// </summary>
public sealed class ShoppingListSourceDay
{
    private ShoppingListSourceDay()
    {
        // EF Core materialization constructor.
    }

    public ShoppingListSourceDay(DateOnly date)
    {
        Date = date;
    }

    public DateOnly Date { get; private set; }
}
