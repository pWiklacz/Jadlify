using Jadlify.Application.Recipes;
using Jadlify.Application.Recipes.GetRecipe;
using Jadlify.Application.Recipes.ListRecipes;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Recipes;

public class RecipeQueryHandlerTests
{
    [Fact]
    public async Task GetRecipe_ReturnsDtoWithTotalAndPerServingMacros()
    {
        Recipe recipe = BuildRecipe();
        var handler = new GetRecipeQueryHandler(new FakeRecipeRepository(recipe));

        Result<RecipeDto> result = await handler.HandleAsync(new GetRecipeQuery(recipe.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(recipe.Id, result.Value.Id);
        Assert.Equal(300m, result.Value.TotalMacros.Calories);
        Assert.Equal(150m, result.Value.PerServingMacros.Calories);
        Assert.Equal(150m, Assert.Single(result.Value.Ingredients).WholeRecipeGrams);
    }

    [Fact]
    public async Task GetRecipe_ReturnsNotFound_WhenRecipeMissing()
    {
        var handler = new GetRecipeQueryHandler(new FakeRecipeRepository());

        Result<RecipeDto> result = await handler.HandleAsync(new GetRecipeQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Recipe.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task ListRecipes_ReturnsRecipeDtos()
    {
        Recipe recipe = BuildRecipe();
        var handler = new ListRecipesQueryHandler(new FakeRecipeRepository(recipe));

        Result<IReadOnlyList<RecipeDto>> result = await handler.HandleAsync(new ListRecipesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        RecipeDto dto = Assert.Single(result.Value);
        Assert.Equal(recipe.Id, dto.Id);
        Assert.Equal(300m, dto.TotalMacros.Calories);
    }

    private static Recipe BuildRecipe()
    {
        var recipe = new Recipe(Guid.NewGuid(), "Porridge", portions: 2);
        recipe.AddIngredient(new RecipeIngredient(
            Guid.NewGuid(),
            "Oats",
            new MacroNutrients(200m, 10m, 5m, 20m),
            new GramAmount(150m)));

        return recipe;
    }
}
