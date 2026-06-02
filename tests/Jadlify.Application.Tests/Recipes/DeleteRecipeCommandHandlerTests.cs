using Jadlify.Application.Recipes.DeleteRecipe;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Recipes;

public class DeleteRecipeCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_DeletesRecipe()
    {
        var recipe = new Recipe(Guid.NewGuid(), "Soup", portions: 1);
        recipe.AddIngredient(new RecipeIngredient(
            Guid.NewGuid(),
            "Carrot",
            new MacroNutrients(41m, 0.9m, 0.2m, 10m),
            new GramAmount(100m)));
        var repository = new FakeRecipeRepository(recipe);
        var handler = new DeleteRecipeCommandHandler(repository);

        Result result = await handler.HandleAsync(new DeleteRecipeCommand(recipe.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(repository.Recipes);
    }

    [Fact]
    public async Task HandleAsync_PropagatesConflict_WhenRecipeIsInUse()
    {
        var repository = new FakeRecipeRepository { DeleteReturnsConflict = true };
        var handler = new DeleteRecipeCommandHandler(repository);

        Result result = await handler.HandleAsync(new DeleteRecipeCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Recipe.InUse", result.Error.Code);
    }
}
