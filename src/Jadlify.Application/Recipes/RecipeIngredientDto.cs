using Jadlify.Domain.Recipes;

namespace Jadlify.Application.Recipes;

public sealed record RecipeIngredientDto(
    Guid ProductId,
    string ProductName,
    decimal WholeRecipeGrams,
    RecipeMacroSummaryDto Per100Grams)
{
    public static RecipeIngredientDto FromDomain(RecipeIngredient ingredient) =>
        new(
            ingredient.ProductId,
            ingredient.ProductName,
            ingredient.WholeRecipeAmount.Value,
            RecipeMacroSummaryDto.FromDomain(ingredient.Per100Grams));
}
