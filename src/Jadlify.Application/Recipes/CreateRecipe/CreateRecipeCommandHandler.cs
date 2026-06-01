using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Products;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Recipes.CreateRecipe;

public sealed class CreateRecipeCommandHandler : ICommandHandler<CreateRecipeCommand, Guid>
{
    private readonly IProductRepository _products;
    private readonly IRecipeRepository _recipes;

    public CreateRecipeCommandHandler(IProductRepository products, IRecipeRepository recipes)
    {
        _products = products;
        _recipes = recipes;
    }

    public async Task<Result<Guid>> HandleAsync(CreateRecipeCommand command, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<RecipeIngredient>> ingredientResult =
            await RecipeIngredientSnapshotFactory.CreateSnapshotsAsync(
                command.Ingredients,
                _products,
                cancellationToken);
        if (ingredientResult.IsFailure)
        {
            return Result.Fail<Guid>(ingredientResult.Error);
        }

        var recipe = new Recipe(Guid.NewGuid(), command.Name, command.Portions);
        recipe.ReplaceDetails(command.Name, command.Portions, ingredientResult.Value);

        await _recipes.AddAsync(recipe, cancellationToken);

        return Result.Ok(recipe.Id);
    }
}
