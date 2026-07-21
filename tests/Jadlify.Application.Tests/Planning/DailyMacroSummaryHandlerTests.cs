using Jadlify.Application.Planning;
using Jadlify.Application.Planning.DailyMacroSummary;
using Jadlify.Application.Tests.Recipes;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Planning;

public class DailyMacroSummaryHandlerTests
{
    private static readonly DateOnly Day = new(2026, 6, 5);

    [Fact]
    public async Task GetDailyMacroSummary_ReturnsPerEntryMacrosTotalAndRemaining()
    {
        Recipe porridge = BuildPorridgeRecipe();
        Recipe lentilSoup = BuildLentilSoupRecipe();
        var breakfast = MealPlanEntry.ForRecipe(Guid.NewGuid(), Day, porridge.Id, MealType.Breakfast, portions: 2);
        var lunch = MealPlanEntry.ForRecipe(Guid.NewGuid(), Day, lentilSoup.Id, MealType.Lunch, portions: 1);
        var goal = new DailyMacroGoal(
            Guid.NewGuid(),
            new MacroNutrients(calories: 400m, protein: 40m, fat: 6m, carbohydrates: 80m));
        var handler = new GetDailyMacroSummaryQueryHandler(
            new FakeMealPlanRepository(breakfast, lunch),
            new FakeRecipeRepository(porridge, lentilSoup),
            new FakeDailyMacroGoalRepository(goal));

        Result<DailyMacroSummaryDto> result =
            await handler.HandleAsync(new GetDailyMacroSummaryQuery(Day), CancellationToken.None);

        Assert.True(result.IsSuccess);
        DailyMacroSummaryDto summary = result.Value;
        Assert.Equal(Day, summary.Date);

        MealEntryMacroDto breakfastMacros = Assert.Single(summary.Entries, entry => entry.EntryId == breakfast.Id);
        Assert.Equal(225m, breakfastMacros.Macros.Calories);
        Assert.Equal(12m, breakfastMacros.Macros.Protein);
        Assert.Equal(5.5m, breakfastMacros.Macros.Fat);
        Assert.Equal(23m, breakfastMacros.Macros.Carbohydrates);

        MealEntryMacroDto lunchMacros = Assert.Single(summary.Entries, entry => entry.EntryId == lunch.Id);
        Assert.Equal(180m, lunchMacros.Macros.Calories);
        Assert.Equal(13.5m, lunchMacros.Macros.Protein);
        Assert.Equal(0.6m, lunchMacros.Macros.Fat);
        Assert.Equal(30m, lunchMacros.Macros.Carbohydrates);

        Assert.Equal(405m, summary.Total.Calories);
        Assert.Equal(25.5m, summary.Total.Protein);
        Assert.Equal(6.1m, summary.Total.Fat);
        Assert.Equal(53m, summary.Total.Carbohydrates);

        Assert.NotNull(summary.Goal);
        Assert.Equal(400m, summary.Goal.Calories);
        Assert.NotNull(summary.Remaining);
        Assert.Equal(-5m, summary.Remaining.Calories);
        Assert.Equal(14.5m, summary.Remaining.Protein);
        Assert.Equal(-0.1m, summary.Remaining.Fat);
        Assert.Equal(27m, summary.Remaining.Carbohydrates);
    }

    [Fact]
    public async Task GetDailyMacroSummary_ReturnsNullGoalAndRemaining_WhenNoGoalConfigured()
    {
        Recipe porridge = BuildPorridgeRecipe();
        var entry = MealPlanEntry.ForRecipe(Guid.NewGuid(), Day, porridge.Id, MealType.Breakfast, portions: 1);
        var handler = new GetDailyMacroSummaryQueryHandler(
            new FakeMealPlanRepository(entry),
            new FakeRecipeRepository(porridge),
            new FakeDailyMacroGoalRepository());

        Result<DailyMacroSummaryDto> result =
            await handler.HandleAsync(new GetDailyMacroSummaryQuery(Day), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Goal);
        Assert.Null(result.Value.Remaining);
        Assert.Equal(112.5m, result.Value.Total.Calories);
    }

    [Fact]
    public async Task GetDailyMacroSummary_ReturnsZeroTotal_ForEmptyDay()
    {
        var handler = new GetDailyMacroSummaryQueryHandler(
            new FakeMealPlanRepository(),
            new FakeRecipeRepository(),
            new FakeDailyMacroGoalRepository());

        Result<DailyMacroSummaryDto> result =
            await handler.HandleAsync(new GetDailyMacroSummaryQuery(Day), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Entries);
        Assert.Equal(0m, result.Value.Total.Calories);
        Assert.Equal(0m, result.Value.Total.Protein);
        Assert.Equal(0m, result.Value.Total.Fat);
        Assert.Equal(0m, result.Value.Total.Carbohydrates);
    }

    [Fact]
    public async Task GetDailyMacroSummary_TreatsMissingOrCrossUserRecipeAsZeroContribution()
    {
        Recipe visibleRecipe = BuildPorridgeRecipe();
        var visibleEntry = MealPlanEntry.ForRecipe(Guid.NewGuid(), Day, visibleRecipe.Id, MealType.Breakfast, portions: 1);
        var hiddenRecipeEntry = MealPlanEntry.ForRecipe(Guid.NewGuid(), Day, Guid.NewGuid(), MealType.Dinner, portions: 3);
        var handler = new GetDailyMacroSummaryQueryHandler(
            new FakeMealPlanRepository(visibleEntry, hiddenRecipeEntry),
            new FakeRecipeRepository(visibleRecipe),
            new FakeDailyMacroGoalRepository());

        Result<DailyMacroSummaryDto> result =
            await handler.HandleAsync(new GetDailyMacroSummaryQuery(Day), CancellationToken.None);

        Assert.True(result.IsSuccess);

        MealEntryMacroDto missingRecipeMacros = Assert.Single(
            result.Value.Entries,
            entry => entry.EntryId == hiddenRecipeEntry.Id);
        Assert.Equal(0m, missingRecipeMacros.Macros.Calories);
        Assert.Equal(0m, missingRecipeMacros.Macros.Protein);
        Assert.Equal(0m, missingRecipeMacros.Macros.Fat);
        Assert.Equal(0m, missingRecipeMacros.Macros.Carbohydrates);
        Assert.Equal(112.5m, result.Value.Total.Calories);
    }

    private static Recipe BuildPorridgeRecipe()
    {
        var recipe = new Recipe(Guid.NewGuid(), "Porridge", portions: 4);
        recipe.AddIngredient(new RecipeIngredient(
            Guid.NewGuid(),
            "Oats",
            new MacroNutrients(200m, 10m, 5m, 20m),
            new GramAmount(200m)));
        recipe.AddIngredient(new RecipeIngredient(
            Guid.NewGuid(),
            "Milk",
            new MacroNutrients(100m, 8m, 2m, 12m),
            new GramAmount(50m)));
        return recipe;
    }

    private static Recipe BuildLentilSoupRecipe()
    {
        var recipe = new Recipe(Guid.NewGuid(), "Lentil soup", portions: 2);
        recipe.AddIngredient(new RecipeIngredient(
            Guid.NewGuid(),
            "Lentils",
            new MacroNutrients(120m, 9m, 0.4m, 20m),
            new GramAmount(300m)));
        return recipe;
    }
}
