using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Shopping.ShoppingLists.ToggleShoppingListItem;

/// <summary>
/// Ticks a single item bought or unbought. <see cref="ExpectedVersion"/> guards against the
/// list having been restructured (refreshed or completed) since it was loaded — a ticking a
/// stale item id is rejected rather than applied to the wrong line. Ticking does not itself bump
/// the version, so sequential ticks never invalidate one another.
/// </summary>
public sealed record ToggleShoppingListItemCommand(
    Guid ListId,
    Guid ItemId,
    bool IsBought,
    int ExpectedVersion) : ICommand<ShoppingListDetailDto>;
