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

/// <summary>
/// One recipe in the catalog index: identity, portions, ingredient count, both macro
/// summaries, and whether it is referenced by a meal-plan entry. Carries no ingredient
/// snapshots — <see cref="RecipeResponse"/> from the detail route is where those live.
/// </summary>
public sealed record RecipeSummaryResponse(
    Guid Id,
    string Name,
    int Portions,
    int IngredientCount,
    RecipeMacroSummaryResponse TotalMacros,
    RecipeMacroSummaryResponse PerServingMacros,
    bool IsInPlan)
{
    public static RecipeSummaryResponse FromDto(RecipeSummaryDto dto) =>
        new(
            dto.Id,
            dto.Name,
            dto.Portions,
            dto.IngredientCount,
            RecipeMacroSummaryResponse.FromDto(dto.TotalMacros),
            RecipeMacroSummaryResponse.FromDto(dto.PerServingMacros),
            dto.IsInPlan);
}

/// <summary>
/// One page of the recipe catalog: <see cref="RecipeSummaryResponse"/> items plus the total
/// match count and the effective paging window (<c>GET /api/recipes/catalog</c>).
/// </summary>
public sealed record RecipeCatalogResponse(
    IReadOnlyList<RecipeSummaryResponse> Items,
    int Total,
    int Skip,
    int Take)
{
    public static RecipeCatalogResponse FromDto(RecipeCatalogPageDto dto) =>
        new(
            dto.Items.Select(RecipeSummaryResponse.FromDto).ToArray(),
            dto.Total,
            dto.Skip,
            dto.Take);
}

public sealed record RecipeResponse(
    Guid Id,
    string Name,
    int Portions,
    IReadOnlyList<RecipeIngredientResponse> Ingredients,
    RecipeMacroSummaryResponse TotalMacros,
    RecipeMacroSummaryResponse PerServingMacros,
    bool IsInPlan)
{
    public static RecipeResponse FromDto(RecipeDto dto) =>
        new(
            dto.Id,
            dto.Name,
            dto.Portions,
            dto.Ingredients.Select(RecipeIngredientResponse.FromDto).ToArray(),
            RecipeMacroSummaryResponse.FromDto(dto.TotalMacros),
            RecipeMacroSummaryResponse.FromDto(dto.PerServingMacros),
            dto.IsInPlan);
}
