using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Recipes;

namespace Jadlify.Application.Recipes;

public sealed record RecipeDto(
    Guid Id,
    string Name,
    int Portions,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    RecipeMacroSummaryDto TotalMacros,
    RecipeMacroSummaryDto PerServingMacros,
    bool IsInPlan)
{
    /// <summary>
    /// Projects the full recipe. <paramref name="isInPlan"/> comes from a batched
    /// owner-scoped meal-plan usage read, never a per-recipe query.
    /// </summary>
    public static RecipeDto FromDomain(Recipe recipe, bool isInPlan)
    {
        MacroNutrients total = MacroCalculator.RecipeTotal(recipe);
        MacroNutrients perServing = MacroCalculator.RecipePerServing(recipe);

        return new RecipeDto(
            recipe.Id,
            recipe.Name,
            recipe.Portions,
            recipe.Ingredients.Select(RecipeIngredientDto.FromDomain).ToList(),
            RecipeMacroSummaryDto.FromDomain(total),
            RecipeMacroSummaryDto.FromDomain(perServing),
            isInPlan);
    }
}
