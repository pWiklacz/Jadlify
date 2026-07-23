using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Shopping.ShoppingLists.GetShoppingListIndex;

/// <summary>Lists the current user's active list and completed history for the index screen.</summary>
public sealed record GetShoppingListIndexQuery : IQuery<ShoppingListIndexDto>;
