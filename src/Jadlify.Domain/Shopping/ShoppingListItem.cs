namespace Jadlify.Domain.Shopping;

public sealed record ShoppingListItem(Guid ProductId, string ProductName, decimal Grams);
