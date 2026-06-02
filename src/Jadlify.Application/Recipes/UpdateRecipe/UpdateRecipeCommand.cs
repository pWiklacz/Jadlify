using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Recipes.UpdateRecipe;

public sealed record UpdateRecipeCommand(
    Guid Id,
    string Name,
    int Portions,
    IReadOnlyList<RecipeIngredientInput> Ingredients) : ICommand;
