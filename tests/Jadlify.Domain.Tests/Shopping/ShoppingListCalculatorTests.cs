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

    [Fact]
    public void ForMealEntries_SameProductNameDifferentIds_RemainSeparateEntries()
    {
        var firstOatsId = Guid.NewGuid();
        var secondOatsId = Guid.NewGuid();
        Recipe recipe = BuildRecipe(
            "Two oats",
            portions: 1,
            // Same ProductName "Oats" but distinct ProductIds: aggregation keys on
            // ProductId, so these must stay as two separate shopping-list entries.
            Ingredient(firstOatsId, "Oats", 100m),
            Ingredient(secondOatsId, "Oats", 50m));
        var entry = new MealPlanEntry(Guid.NewGuid(), Day, recipe.Id, MealType.Breakfast, portions: 1);

        IReadOnlyList<ShoppingListItem> result = ShoppingListCalculator.ForMealEntries([(entry, recipe)]);

        Assert.Equal(2, result.Count);
        ShoppingListItem first = Assert.Single(result, item => item.ProductId == firstOatsId);
        ShoppingListItem second = Assert.Single(result, item => item.ProductId == secondOatsId);
        // Oracle: factor = 1/1 = 1, so grams pass through unchanged per ingredient.
        Assert.Equal(100m, first.Grams);
        Assert.Equal(50m, second.Grams);
    }

    [Fact]
    public void ForMealEntries_SameProductIdDifferentSnapshotNames_UsesFirstSeenName()
    {
        var sharedProductId = Guid.NewGuid();
        Recipe firstRecipe = BuildRecipe(
            "Older recipe",
            portions: 1,
            Ingredient(sharedProductId, "Original oats", 100m));
        Recipe secondRecipe = BuildRecipe(
            "Newer recipe",
            portions: 1,
            Ingredient(sharedProductId, "Renamed oats", 50m));
        var firstEntry = new MealPlanEntry(Guid.NewGuid(), Day, firstRecipe.Id, MealType.Breakfast, portions: 1);
        var secondEntry = new MealPlanEntry(Guid.NewGuid(), Day, secondRecipe.Id, MealType.Lunch, portions: 1);

        ShoppingListItem item = Assert.Single(ShoppingListCalculator.ForMealEntries(
            [(firstEntry, firstRecipe), (secondEntry, secondRecipe)]));

        Assert.Equal(sharedProductId, item.ProductId);
        // First-encountered snapshot name wins for an aggregated product.
        Assert.Equal("Original oats", item.ProductName);
        // Oracle: 100 × (1/1) + 50 × (1/1) = 150.
        Assert.Equal(150m, item.Grams);
    }

    [Fact]
    public void ForMealEntries_AggregatedGrams_MatchIndependentOracle()
    {
        var pastaId = Guid.NewGuid();
        var tomatoId = Guid.NewGuid();
        var oilId = Guid.NewGuid();
        Recipe pastaDish = BuildRecipe(
            "Pasta dish",
            portions: 4,
            Ingredient(pastaId, "Pasta", 500m),
            Ingredient(tomatoId, "Tomato", 300m));
        Recipe salad = BuildRecipe(
            "Salad",
            portions: 2,
            Ingredient(tomatoId, "Tomato", 150m),
            Ingredient(oilId, "Oil", 50m));
        var pastaEntry = new MealPlanEntry(Guid.NewGuid(), Day, pastaDish.Id, MealType.Dinner, portions: 2);
        var saladEntry = new MealPlanEntry(Guid.NewGuid(), Day, salad.Id, MealType.Lunch, portions: 3);

        IReadOnlyList<ShoppingListItem> result = ShoppingListCalculator.ForMealEntries(
            [(pastaEntry, pastaDish), (saladEntry, salad)]);

        // Oracle: scaled_grams = wholeRecipeAmount × (entryPortions / recipePortions),
        // then sum per ProductId.
        // Pasta:  500 × (2/4) = 250
        // Tomato: 300 × (2/4) + 150 × (3/2) = 150 + 225 = 375
        // Oil:     50 × (3/2) = 75
        ShoppingListItem pasta = Assert.Single(result, item => item.ProductId == pastaId);
        ShoppingListItem tomato = Assert.Single(result, item => item.ProductId == tomatoId);
        ShoppingListItem oil = Assert.Single(result, item => item.ProductId == oilId);
        Assert.Equal(250m, pasta.Grams);
        Assert.Equal(375m, tomato.Grams);
        Assert.Equal(75m, oil.Grams);
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
