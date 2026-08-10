using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Shopping.ShoppingLists.GetShoppingListDetail;

/// <summary>Returns one shopping list in full for the current user, active or completed.</summary>
public sealed record GetShoppingListDetailQuery(Guid Id) : IQuery<ShoppingListDetailDto>;
