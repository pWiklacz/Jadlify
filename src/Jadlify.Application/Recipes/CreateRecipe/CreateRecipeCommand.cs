using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Recipes.CreateRecipe;

public sealed record CreateRecipeCommand(
    string Name,
    int Portions,
    IReadOnlyList<RecipeIngredientInput> Ingredients) : ICommand<Guid>;
