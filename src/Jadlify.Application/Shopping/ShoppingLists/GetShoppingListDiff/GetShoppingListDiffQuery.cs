using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Shopping.ShoppingLists.GetShoppingListDiff;

/// <summary>
/// Previews what a refresh of the active list would change, computed from its source days
/// without writing anything. The result carries the version and fingerprint to confirm against.
/// </summary>
public sealed record GetShoppingListDiffQuery(Guid Id) : IQuery<ShoppingListDiffDto>;
