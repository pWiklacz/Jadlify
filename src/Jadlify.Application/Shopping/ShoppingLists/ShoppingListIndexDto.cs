namespace Jadlify.Application.Shopping.ShoppingLists;

/// <summary>
/// The shopping-list index: the one active list (or <c>null</c>) and the completed history in
/// reverse-chronological order. Splitting them here keeps the UI from re-deriving which list is
/// live from a flat collection.
/// </summary>
public sealed record ShoppingListIndexDto(
    ShoppingListSummaryDto? Active,
    IReadOnlyList<ShoppingListSummaryDto> History);
