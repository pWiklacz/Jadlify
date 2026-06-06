namespace Jadlify.Application.Shopping;

public sealed record ShoppingListWarningDto(Guid EntryId, Guid RecipeId, string Message);
