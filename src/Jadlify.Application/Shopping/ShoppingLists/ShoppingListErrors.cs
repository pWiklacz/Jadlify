using Jadlify.SharedKernel;

namespace Jadlify.Application.Shopping.ShoppingLists;

/// <summary>
/// The shared failure vocabulary for shopping-list operations, mapped to HTTP status by
/// <c>ResultExtensions.ToProblem</c>: not-found variants become 404 and the conflicts become
/// 409. Kept in one place so every handler returns identical codes and messages.
/// </summary>
public static class ShoppingListErrors
{
    public static readonly Error NotFound =
        Error.NotFound("ShoppingList.NotFound", "The shopping list was not found for the current user.");

    public static readonly Error ItemNotFound =
        Error.NotFound("ShoppingList.ItemNotFound", "The shopping-list item was not found.");

    public static readonly Error ActiveExists =
        Error.Conflict("ShoppingList.ActiveExists", "You already have an active shopping list.");

    public static readonly Error NotActive =
        Error.Conflict("ShoppingList.NotActive", "This shopping list is completed and cannot be changed.");

    public static readonly Error VersionConflict =
        Error.Conflict(
            "ShoppingList.VersionConflict",
            "The shopping list changed since you last loaded it. Reload it and try again.");

    public static readonly Error SourceChanged =
        Error.Conflict(
            "ShoppingList.SourceChanged",
            "The plan changed again since this preview was generated. Review the new changes and try again.");
}
