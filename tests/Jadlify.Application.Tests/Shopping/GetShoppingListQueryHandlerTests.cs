using Jadlify.Application.Shopping;
using Jadlify.Application.Shopping.GetShoppingList;
using Jadlify.Application.Tests.Planning;
using Jadlify.Application.Tests.Recipes;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Shopping;

public class GetShoppingListQueryHandlerTests
{
    private static readonly DateOnly Day = new(2026, 6, 6);

    [Fact]
    public async Task GetShoppingList_ReturnsEmptyList_ForEmptyDay()
    {
        var handler = new GetShoppingListQueryHandler(
            new FakeMealPlanRepository(),
            new FakeRecipeRepository());

        Result<ShoppingListDto> result =
            await handler.HandleAsync(new GetShoppingListQuery(Day), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Day, result.Value.Date);
        Assert.Empty(result.Value.Items);
        Assert.Empty(result.Value.Warnings);
    }

    [Fact]
    public async Task GetShoppingList_AggregatesDuplicateProductsAcrossEntries()
    {
        var sharedProductId = Guid.NewGuid();
        Recipe porridge = BuildRecipe(
            "Porridge",
            portions: 4,
            Ingredient(sharedProductId, "Oats", 400m),
            Ingredient(Guid.NewGuid(), "Milk", 200m));
        Recipe pancakes = BuildRecipe(
            "Pancakes",
            portions: 2,
            Ingredient(sharedProductId, "Oats", 100m),
            Ingredient(Guid.NewGuid(), "Eggs", 120m));
        var breakfast = new MealPlanEntry(Guid.NewGuid(), Day, porridge.Id, MealType.Breakfast, portions: 2);
        var lunch = new MealPlanEntry(Guid.NewGuid(), Day, pancakes.Id, MealType.Lunch, portions: 1);
        var dinner = new MealPlanEntry(Guid.NewGuid(), Day, porridge.Id, MealType.Dinner, portions: 1);
        var handler = new GetShoppingListQueryHandler(
            new FakeMealPlanRepository(breakfast, lunch, dinner),
            new FakeRecipeRepository(porridge, pancakes));

        Result<ShoppingListDto> result =
            await handler.HandleAsync(new GetShoppingListQuery(Day), CancellationToken.None);

        Assert.True(result.IsSuccess);
        ShoppingListItemDto oats = Assert.Single(result.Value.Items, item => item.ProductId == sharedProductId);
        Assert.Equal("Oats", oats.ProductName);
        Assert.Equal(350m, oats.Grams);
    }

    [Fact]
    public async Task GetShoppingList_SortsItemsByNameThenProductId()
    {
        var earlyAppleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var lateAppleId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        Recipe recipe = BuildRecipe(
            "Sorted",
            portions: 1,
            Ingredient(Guid.NewGuid(), "Carrots", 100m),
            Ingredient(lateAppleId, "Apple", 100m),
            Ingredient(earlyAppleId, "apple", 100m));
        var entry = new MealPlanEntry(Guid.NewGuid(), Day, recipe.Id, MealType.Lunch, portions: 1);
        var handler = new GetShoppingListQueryHandler(
            new FakeMealPlanRepository(entry),
            new FakeRecipeRepository(recipe));

        Result<ShoppingListDto> result =
            await handler.HandleAsync(new GetShoppingListQuery(Day), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Collection(
            result.Value.Items,
            item => Assert.Equal(earlyAppleId, item.ProductId),
            item => Assert.Equal(lateAppleId, item.ProductId),
            item => Assert.Equal("Carrots", item.ProductName));
    }

    [Fact]
    public async Task GetShoppingList_AddsWarningAndSkipsContribution_ForMissingRecipe()
    {
        Recipe visibleRecipe = BuildRecipe(
            "Soup",
            portions: 2,
            Ingredient(Guid.NewGuid(), "Lentils", 300m));
        var visibleEntry = new MealPlanEntry(Guid.NewGuid(), Day, visibleRecipe.Id, MealType.Lunch, portions: 1);
        var missingRecipeId = Guid.NewGuid();
        var missingEntry = new MealPlanEntry(Guid.NewGuid(), Day, missingRecipeId, MealType.Dinner, portions: 3);
        var handler = new GetShoppingListQueryHandler(
            new FakeMealPlanRepository(visibleEntry, missingEntry),
            new FakeRecipeRepository(visibleRecipe));

        Result<ShoppingListDto> result =
            await handler.HandleAsync(new GetShoppingListQuery(Day), CancellationToken.None);

        Assert.True(result.IsSuccess);
        ShoppingListItemDto item = Assert.Single(result.Value.Items);
        Assert.Equal("Lentils", item.ProductName);
        Assert.Equal(150m, item.Grams);

        ShoppingListWarningDto warning = Assert.Single(result.Value.Warnings);
        Assert.Equal(missingEntry.Id, warning.EntryId);
        Assert.Equal(missingRecipeId, warning.RecipeId);
        Assert.Contains("not found", warning.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetShoppingList_FiltersEntriesBySelectedDateThroughRepository()
    {
        Recipe recipe = BuildRecipe(
            "Porridge",
            portions: 1,
            Ingredient(Guid.NewGuid(), "Oats", 100m));
        var selectedDayEntry = new MealPlanEntry(Guid.NewGuid(), Day, recipe.Id, MealType.Breakfast, portions: 1);
        var otherDayEntry = new MealPlanEntry(Guid.NewGuid(), Day.AddDays(1), recipe.Id, MealType.Breakfast, portions: 1);
        var handler = new GetShoppingListQueryHandler(
            new FakeMealPlanRepository(selectedDayEntry, otherDayEntry),
            new FakeRecipeRepository(recipe));

        Result<ShoppingListDto> result =
            await handler.HandleAsync(new GetShoppingListQuery(Day), CancellationToken.None);

        Assert.True(result.IsSuccess);
        ShoppingListItemDto item = Assert.Single(result.Value.Items);
        Assert.Equal(100m, item.Grams);
    }

    [Fact]
    public async Task GetShoppingList_UsesIngredientSnapshotNames()
    {
        Recipe recipe = BuildRecipe(
            "Historical",
            portions: 1,
            Ingredient(Guid.NewGuid(), "Original oats", 100m));
        var entry = new MealPlanEntry(Guid.NewGuid(), Day, recipe.Id, MealType.Breakfast, portions: 1);
        var handler = new GetShoppingListQueryHandler(
            new FakeMealPlanRepository(entry),
            new FakeRecipeRepository(recipe));

        Result<ShoppingListDto> result =
            await handler.HandleAsync(new GetShoppingListQuery(Day), CancellationToken.None);

        Assert.True(result.IsSuccess);
        ShoppingListItemDto item = Assert.Single(result.Value.Items);
        Assert.Equal("Original oats", item.ProductName);
    }

    private static Recipe BuildRecipe(string name, int portions, params RecipeIngredient[] ingredients)
    {
        var recipe = new Recipe(Guid.NewGuid(), name, portions);

        foreach (RecipeIngredient ingredient in ingredients)
        {
            recipe.AddIngredient(ingredient);
        }

        return recipe;
    }

    private static RecipeIngredient Ingredient(Guid productId, string name, decimal grams) =>
        new(productId, name, new MacroNutrients(100m, 10m, 1m, 20m), new GramAmount(grams));
}
