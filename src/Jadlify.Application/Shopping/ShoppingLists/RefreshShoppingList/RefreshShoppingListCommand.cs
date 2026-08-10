using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Shopping.ShoppingLists.RefreshShoppingList;

/// <summary>
/// Applies the changes a preview showed. <see cref="ExpectedVersion"/> must match the list and
/// <see cref="ExpectedSourceFingerprint"/> must match a freshly recomputed projection, so an
/// apply based on a stale preview — the list moved on, or the plan changed again — is rejected
/// with a conflict rather than writing an unseen diff.
/// </summary>
public sealed record RefreshShoppingListCommand(
    Guid Id,
    int ExpectedVersion,
    string ExpectedSourceFingerprint) : ICommand<ShoppingListDetailDto>;
