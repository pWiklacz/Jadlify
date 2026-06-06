using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Shopping.GetShoppingList;

public sealed record GetShoppingListQuery(DateOnly Date) : IQuery<ShoppingListDto>;
