using Jadlify.Application.Recipes;
using Jadlify.Application.Recipes.CreateRecipe;
using Jadlify.Application.Tests.Products;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Products;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Recipes;

public class CreateRecipeCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_AddsRecipeWithProductSnapshots()
    {
        var product = new Product(Guid.NewGuid(), "Skyr", new MacroNutrients(63m, 11m, 0.2m, 4m));
        var productRepository = new FakeProductRepository(product);
        var recipeRepository = new FakeRecipeRepository();
        var handler = new CreateRecipeCommandHandler(productRepository, recipeRepository);
        var command = new CreateRecipeCommand(
            "Protein bowl",
            2,
            [new RecipeIngredientInput(product.Id, 150m)]);

        Result<Guid> result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Recipe recipe = Assert.Single(recipeRepository.Recipes);
        Assert.Equal(result.Value, recipe.Id);
        Assert.Equal("Protein bowl", recipe.Name);
        Assert.Equal(2, recipe.Portions);

        RecipeIngredient ingredient = Assert.Single(recipe.Ingredients);
        Assert.Equal(product.Id, ingredient.ProductId);
        Assert.Equal("Skyr", ingredient.ProductName);
        Assert.Equal(product.Per100Grams, ingredient.Per100Grams);
        Assert.Equal(150m, ingredient.WholeRecipeAmount.Value);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValidationFailure_WhenIngredientProductIsMissing()
    {
        var handler = new CreateRecipeCommandHandler(new FakeProductRepository(), new FakeRecipeRepository());
        var command = new CreateRecipeCommand(
            "Ghost recipe",
            1,
            [new RecipeIngredientInput(Guid.NewGuid(), 100m)]);

        Result<Guid> result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }
}
