using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Recipes;
using Jadlify.Domain.Shopping;

namespace Jadlify.Domain.Tests.Shopping;

public class ShoppingListCalculatorTests
{
    private static readonly DateOnly Day = new(2026, 6, 6);

    [Fact]
    public void ForMealEntries_ReturnsEmptyList_ForEmptyInput()
    {
        IReadOnlyList<ShoppingListItem> result = ShoppingListCalculator.ForMealEntries([]);

        Assert.Empty(result);
    }

    [Fact]
    public void ForMealEntries_ScalesWholeRecipeAmountByPlannedPortions()
    {
        var productId = Guid.NewGuid();
        Recipe recipe = BuildRecipe(
            "Porridge",
            portions: 4,
            Ingredient(productId, "Oats", 400m));
        var entry = new MealPlanEntry(Guid.NewGuid(), Day, recipe.Id, MealType.Breakfast, portions: 2);

        ShoppingListItem item = Assert.Single(ShoppingListCalculator.ForMealEntries([(entry, recipe)]));

        Assert.Equal(productId, item.ProductId);
        Assert.Equal("Oats", item.ProductName);
        Assert.Equal(200m, item.Grams);
    }

    [Fact]
    public void ForMealEntries_SumsDuplicateProductsAcrossRecipesAndEntries()
    {
        var sharedProductId = Guid.NewGuid();
        Recipe porridge = BuildRecipe(
            "Porridge",
            portions: 4,
            Ingredient(sharedProductId, "Oats", 400m),
            Ingredient(Guid.NewGuid(), "Milk", 200m));
        Recipe overnightOats = BuildRecipe(
            "Overnight oats",
            portions: 2,
            Ingredient(sharedProductId, "Oats", 100m));
        var breakfast = new MealPlanEntry(Guid.NewGuid(), Day, porridge.Id, MealType.Breakfast, portions: 2);
        var snack = new MealPlanEntry(Guid.NewGuid(), Day, overnightOats.Id, MealType.Snack, portions: 1);
        var secondBreakfast = new MealPlanEntry(Guid.NewGuid(), Day, porridge.Id, MealType.Lunch, portions: 1);

        IReadOnlyList<ShoppingListItem> result = ShoppingListCalculator.ForMealEntries(
            [(breakfast, porridge), (snack, overnightOats), (secondBreakfast, porridge)]);

        ShoppingListItem oats = Assert.Single(result, item => item.ProductId == sharedProductId);
        Assert.Equal(350m, oats.Grams);
    }

    [Fact]
    public void ForMealEntries_DoesNotRoundBeforeReturning()
    {
        Recipe recipe = BuildRecipe(
            "Soup",
            portions: 3,
            Ingredient(Guid.NewGuid(), "Lentils", 100m));
        var entry = new MealPlanEntry(Guid.NewGuid(), Day, recipe.Id, MealType.Dinner, portions: 2);

        ShoppingListItem item = Assert.Single(ShoppingListCalculator.ForMealEntries([(entry, recipe)]));

        Assert.Equal(66.666666666666666666666666670m, item.Grams);
    }

    [Fact]
    public void ForMealEntries_ReturnsItemsSortedByNameThenProductId()
    {
        var earlyAppleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var lateAppleId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        Recipe recipe = BuildRecipe(
            "Sorted",
            portions: 1,
            Ingredient(Guid.NewGuid(), "banana", 10m),
            Ingredient(lateAppleId, "Apple", 10m),
            Ingredient(earlyAppleId, "apple", 10m));
        var entry = new MealPlanEntry(Guid.NewGuid(), Day, recipe.Id, MealType.Breakfast, portions: 1);

        IReadOnlyList<ShoppingListItem> result = ShoppingListCalculator.ForMealEntries([(entry, recipe)]);

        Assert.Collection(
            result,
            item => Assert.Equal(earlyAppleId, item.ProductId),
            item => Assert.Equal(lateAppleId, item.ProductId),
            item => Assert.Equal("banana", item.ProductName));
    }

    [Fact]
    public void ForMealEntries_UsesIngredientSnapshotName()
    {
        Recipe recipe = BuildRecipe(
            "Historical",
            portions: 1,
            Ingredient(Guid.NewGuid(), "Original oats", 100m));
        var entry = new MealPlanEntry(Guid.NewGuid(), Day, recipe.Id, MealType.Breakfast, portions: 1);

        ShoppingListItem item = Assert.Single(ShoppingListCalculator.ForMealEntries([(entry, recipe)]));

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
