using Jadlify.Application.Recipes;
using Jadlify.Application.Recipes.UpdateRecipe;
using Jadlify.Application.Tests.Products;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Products;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Recipes;

public class UpdateRecipeCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReplacesCompleteRecipeComposition()
    {
        var recipeId = Guid.NewGuid();
        var product = new Product(Guid.NewGuid(), "Rice", new MacroNutrients(130m, 2.7m, 0.3m, 28m));
        var existing = new Recipe(recipeId, "Old", portions: 1);
        existing.AddIngredient(new RecipeIngredient(
            Guid.NewGuid(),
            "Old product",
            new MacroNutrients(1m, 1m, 1m, 1m),
            new GramAmount(10m)));

        var recipeRepository = new FakeRecipeRepository(existing);
        var handler = new UpdateRecipeCommandHandler(
            new FakeProductRepository(product),
            recipeRepository);
        var command = new UpdateRecipeCommand(
            recipeId,
            "Rice bowl",
            3,
            [new RecipeIngredientInput(product.Id, 250m)]);

        Result result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Recipe updated = Assert.Single(recipeRepository.Recipes);
        Assert.Equal("Rice bowl", updated.Name);
        Assert.Equal(3, updated.Portions);

        RecipeIngredient ingredient = Assert.Single(updated.Ingredients);
        Assert.Equal(product.Id, ingredient.ProductId);
        Assert.Equal("Rice", ingredient.ProductName);
        Assert.Equal(250m, ingredient.WholeRecipeAmount.Value);
    }

    [Fact]
    public async Task HandleAsync_PropagatesNotFound_WhenRecipeIsMissing()
    {
        var product = new Product(Guid.NewGuid(), "Rice", new MacroNutrients(130m, 2.7m, 0.3m, 28m));
        var handler = new UpdateRecipeCommandHandler(new FakeProductRepository(product), new FakeRecipeRepository());
        var command = new UpdateRecipeCommand(
            Guid.NewGuid(),
            "Rice bowl",
            3,
            [new RecipeIngredientInput(product.Id, 250m)]);

        Result result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Recipe.NotFound", result.Error.Code);
    }
}
