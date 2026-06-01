using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Products;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Recipes.UpdateRecipe;

public sealed class UpdateRecipeCommandHandler : ICommandHandler<UpdateRecipeCommand>
{
    private readonly IProductRepository _products;
    private readonly IRecipeRepository _recipes;

    public UpdateRecipeCommandHandler(IProductRepository products, IRecipeRepository recipes)
    {
        _products = products;
        _recipes = recipes;
    }

    public async Task<Result> HandleAsync(UpdateRecipeCommand command, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<RecipeIngredient>> ingredientResult =
            await RecipeIngredientSnapshotFactory.CreateSnapshotsAsync(
                command.Ingredients,
                _products,
                cancellationToken);
        if (ingredientResult.IsFailure)
        {
            return Result.Fail(ingredientResult.Error);
        }

        var recipe = new Recipe(command.Id, command.Name, command.Portions);
        recipe.ReplaceDetails(command.Name, command.Portions, ingredientResult.Value);

        return await _recipes.UpdateAsync(recipe, cancellationToken);
    }
}
