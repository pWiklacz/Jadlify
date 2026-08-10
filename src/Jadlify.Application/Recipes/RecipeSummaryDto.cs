using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Recipes;

namespace Jadlify.Application.Recipes;

/// <summary>
/// The light catalog projection of a recipe: identity, portion count, how many
/// ingredients it has, both macro summaries, and whether any meal-plan entry
/// references it. Deliberately omits the ingredient snapshots that
/// <see cref="RecipeDto"/> carries so an index page never ships the full
/// composition of every recipe — the detail endpoint remains the place for that.
/// </summary>
public sealed record RecipeSummaryDto(
    Guid Id,
    string Name,
    int Portions,
    int IngredientCount,
    RecipeMacroSummaryDto TotalMacros,
    RecipeMacroSummaryDto PerServingMacros,
    bool IsInPlan)
{
    /// <summary>
    /// Projects one recipe. <paramref name="isInPlan"/> comes from a single batched
    /// usage read over the whole page, never a per-recipe query.
    /// </summary>
    public static RecipeSummaryDto FromDomain(Recipe recipe, bool isInPlan)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        MacroNutrients total = MacroCalculator.RecipeTotal(recipe);
        MacroNutrients perServing = MacroCalculator.RecipePerServing(recipe);

        return new RecipeSummaryDto(
            recipe.Id,
            recipe.Name,
            recipe.Portions,
            recipe.Ingredients.Count,
            RecipeMacroSummaryDto.FromDomain(total),
            RecipeMacroSummaryDto.FromDomain(perServing),
            isInPlan);
    }
}
