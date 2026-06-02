using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Recipes;

namespace Jadlify.Application.Recipes;

public sealed record RecipeDto(
    Guid Id,
    string Name,
    int Portions,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    RecipeMacroSummaryDto TotalMacros,
    RecipeMacroSummaryDto PerServingMacros)
{
    public static RecipeDto FromDomain(Recipe recipe)
    {
        MacroNutrients total = MacroCalculator.RecipeTotal(recipe);
        MacroNutrients perServing = MacroCalculator.RecipePerServing(recipe);

        return new RecipeDto(
            recipe.Id,
            recipe.Name,
            recipe.Portions,
            recipe.Ingredients.Select(RecipeIngredientDto.FromDomain).ToList(),
            RecipeMacroSummaryDto.FromDomain(total),
            RecipeMacroSummaryDto.FromDomain(perServing));
    }
}
