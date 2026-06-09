using Jadlify.Application.Shopping;

namespace Jadlify.API.Shopping;

public sealed record ShoppingListResponse(
    DateOnly Date,
    IReadOnlyList<ShoppingListItemResponse> Items,
    IReadOnlyList<ShoppingListWarningResponse> Warnings)
{
    public static ShoppingListResponse FromDto(ShoppingListDto dto) =>
        new(
            dto.Date,
            dto.Items.Select(ShoppingListItemResponse.FromDto).ToArray(),
            dto.Warnings.Select(ShoppingListWarningResponse.FromDto).ToArray());
}

public sealed record ShoppingListItemResponse(
    Guid ProductId,
    string ProductName,
    decimal Grams)
{
    public static ShoppingListItemResponse FromDto(ShoppingListItemDto dto) =>
        new(dto.ProductId, dto.ProductName, dto.Grams);
}

public sealed record ShoppingListWarningResponse(
    Guid EntryId,
    Guid RecipeId,
    string Message)
{
    public static ShoppingListWarningResponse FromDto(ShoppingListWarningDto dto) =>
        new(dto.EntryId, dto.RecipeId, dto.Message);
}
