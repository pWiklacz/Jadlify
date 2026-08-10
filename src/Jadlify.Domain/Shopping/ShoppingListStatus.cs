namespace Jadlify.Domain.Shopping;

/// <summary>
/// Lifecycle of a persistent shopping list. Members use stable English names because the wire
/// contract and the persisted value both key off them; the UI maps each to a Polish label.
/// </summary>
public enum ShoppingListStatus
{
    /// <summary>The one live list a user shops from: items can be ticked off and it can be refreshed.</summary>
    Active,

    /// <summary>A finished list, kept as a frozen snapshot for history. Immutable once completed.</summary>
    Completed,
}
