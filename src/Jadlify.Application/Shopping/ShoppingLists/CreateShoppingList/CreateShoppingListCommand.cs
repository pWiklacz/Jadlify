using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Shopping.ShoppingLists.CreateShoppingList;

/// <summary>
/// Creates a new active shopping list from an arbitrary, bounded set of <see cref="Days"/>. The
/// items are projected from the plan on those days at creation time and frozen into the list.
/// Fails with a conflict if the user already has an active list.
/// </summary>
public sealed record CreateShoppingListCommand(
    string Name,
    IReadOnlyList<DateOnly> Days) : ICommand<ShoppingListDetailDto>;
