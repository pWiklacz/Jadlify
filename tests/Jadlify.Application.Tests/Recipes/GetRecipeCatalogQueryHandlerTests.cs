using Jadlify.Application.Recipes;
using Jadlify.Application.Recipes.GetRecipeCatalog;
using Jadlify.Application.Tests.Planning;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Recipes;

public class GetRecipeCatalogQueryHandlerTests
{
    [Fact]
    public async Task Handle_ProjectsSummariesWithIngredientCountAndMacros()
    {
        // Oracle: 150 g of a 200 kcal/100 g product = 200 * 1.5 = 300 kcal total.
        // Portions = 2, so per serving = 300 / 2 = 150 kcal.
        Recipe recipe = BuildRecipe("Porridge", portions: 2, calories: 200m, grams: 150m);
        GetRecipeCatalogQueryHandler handler = BuildHandler(new FakeRecipeRepository(recipe));

        Result<RecipeCatalogPageDto> result =
            await handler.HandleAsync(new GetRecipeCatalogQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        RecipeSummaryDto summary = Assert.Single(result.Value.Items);
        Assert.Equal(recipe.Id, summary.Id);
        Assert.Equal("Porridge", summary.Name);
        Assert.Equal(2, summary.Portions);
        Assert.Equal(1, summary.IngredientCount);
        Assert.Equal(300m, summary.TotalMacros.Calories);
        Assert.Equal(150m, summary.PerServingMacros.Calories);
        Assert.False(summary.IsInPlan);
    }

    [Fact]
    public async Task Handle_EchoesPagingWindowAndTotal()
    {
        FakeRecipeRepository repository = new(
            BuildRecipe("A", 1, 100m, 100m),
            BuildRecipe("B", 1, 100m, 100m),
            BuildRecipe("C", 1, 100m, 100m));
        GetRecipeCatalogQueryHandler handler = BuildHandler(repository);

        Result<RecipeCatalogPageDto> result = await handler.HandleAsync(
            new GetRecipeCatalogQuery(Skip: 1, Take: 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Total);
        Assert.Equal(1, result.Value.Skip);
        Assert.Equal(1, result.Value.Take);
        Assert.Equal("B", Assert.Single(result.Value.Items).Name);
    }

    [Fact]
    public async Task Handle_ClampsTakeToMaxBound()
    {
        GetRecipeCatalogQueryHandler handler = BuildHandler(new FakeRecipeRepository());

        Result<RecipeCatalogPageDto> result = await handler.HandleAsync(
            new GetRecipeCatalogQuery(Take: RecipeListBounds.MaxTake + 500),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(RecipeListBounds.MaxTake, result.Value.Take);
    }

    [Fact]
    public async Task Handle_FiltersBySearch()
    {
        FakeRecipeRepository repository = new(
            BuildRecipe("Owsianka", 1, 100m, 100m),
            BuildRecipe("Kurczak z ryżem", 1, 100m, 100m));
        GetRecipeCatalogQueryHandler handler = BuildHandler(repository);

        Result<RecipeCatalogPageDto> result = await handler.HandleAsync(
            new GetRecipeCatalogQuery(Search: "kurczak"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Total);
        Assert.Equal("Kurczak z ryżem", Assert.Single(result.Value.Items).Name);
    }

    [Fact]
    public async Task Handle_MarksOnlyPlannedRecipesAsInPlan()
    {
        Recipe planned = BuildRecipe("Planned", 1, 100m, 100m);
        Recipe unplanned = BuildRecipe("Unplanned", 1, 100m, 100m);
        FakeMealPlanRepository mealPlan = new(new MealPlanEntry(
            Guid.NewGuid(),
            new DateOnly(2026, 7, 21),
            planned.Id,
            MealType.Breakfast,
            portions: 1));
        GetRecipeCatalogQueryHandler handler =
            new(new FakeRecipeRepository(planned, unplanned), mealPlan);

        Result<RecipeCatalogPageDto> result =
            await handler.HandleAsync(new GetRecipeCatalogQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Items.Single(item => item.Name == "Planned").IsInPlan);
        Assert.False(result.Value.Items.Single(item => item.Name == "Unplanned").IsInPlan);
    }

    private static GetRecipeCatalogQueryHandler BuildHandler(FakeRecipeRepository recipes) =>
        new(recipes, new FakeMealPlanRepository());

    private static Recipe BuildRecipe(string name, int portions, decimal calories, decimal grams)
    {
        Recipe recipe = new(Guid.NewGuid(), name, portions);
        recipe.AddIngredient(new RecipeIngredient(
            Guid.NewGuid(),
            "Product",
            new MacroNutrients(calories, 10m, 5m, 20m),
            new GramAmount(grams)));

        return recipe;
    }
}
