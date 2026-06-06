namespace Jadlify.Application.Shopping;

public sealed record ShoppingListDto(
    DateOnly Date,
    IReadOnlyList<ShoppingListItemDto> Items,
    IReadOnlyList<ShoppingListWarningDto> Warnings);
