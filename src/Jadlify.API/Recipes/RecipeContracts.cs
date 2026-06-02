using Jadlify.Application.Recipes;

namespace Jadlify.API.Recipes;

public sealed record RecipeIngredientRequest(Guid ProductId, decimal WholeRecipeGrams);

public sealed record CreateRecipeRequest(
    string Name,
    int Portions,
    IReadOnlyList<RecipeIngredientRequest>? Ingredients);

public sealed record UpdateRecipeRequest(
    string Name,
    int Portions,
    IReadOnlyList<RecipeIngredientRequest>? Ingredients);

public sealed record CreatedRecipeResponse(Guid Id);

public sealed record RecipeMacroSummaryResponse(
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbohydrates)
{
    public static RecipeMacroSummaryResponse FromDto(RecipeMacroSummaryDto dto) =>
        new(dto.Calories, dto.Protein, dto.Fat, dto.Carbohydrates);
}

public sealed record RecipeIngredientResponse(
    Guid ProductId,
    string ProductName,
    decimal WholeRecipeGrams,
    RecipeMacroSummaryResponse Per100Grams)
{
    public static RecipeIngredientResponse FromDto(RecipeIngredientDto dto) =>
        new(
            dto.ProductId,
            dto.ProductName,
            dto.WholeRecipeGrams,
            RecipeMacroSummaryResponse.FromDto(dto.Per100Grams));
}

public sealed record RecipeResponse(
    Guid Id,
    string Name,
    int Portions,
    IReadOnlyList<RecipeIngredientResponse> Ingredients,
    RecipeMacroSummaryResponse TotalMacros,
    RecipeMacroSummaryResponse PerServingMacros)
{
    public static RecipeResponse FromDto(RecipeDto dto) =>
        new(
            dto.Id,
            dto.Name,
            dto.Portions,
            dto.Ingredients.Select(RecipeIngredientResponse.FromDto).ToArray(),
            RecipeMacroSummaryResponse.FromDto(dto.TotalMacros),
            RecipeMacroSummaryResponse.FromDto(dto.PerServingMacros));
}
