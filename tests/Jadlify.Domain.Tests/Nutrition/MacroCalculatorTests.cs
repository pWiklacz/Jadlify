using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Products;
using Jadlify.Domain.Recipes;

namespace Jadlify.Domain.Tests.Nutrition;

public class MacroCalculatorTests
{
    [Fact]
    public void ForProductAmount_ScalesValuesProportionallyToGrams()
    {
        var product = new Product(Guid.NewGuid(), "Oats", new MacroNutrients(200m, 10m, 5m, 20m));

        MacroNutrients result = MacroCalculator.ForProductAmount(product, new GramAmount(150m));

        Assert.Equal(new MacroNutrients(300m, 15m, 7.5m, 30m), result);
    }

    [Fact]
    public void ForProductAmount_AtHundredGrams_ReturnsPer100gValues()
    {
        var per100g = new MacroNutrients(200m, 10m, 5m, 20m);
        var product = new Product(Guid.NewGuid(), "Oats", per100g);

        MacroNutrients result = MacroCalculator.ForProductAmount(product, new GramAmount(100m));

        Assert.Equal(per100g, result);
    }

    [Fact]
    public void RecipeTotal_SumsWholeRecipeIngredients()
    {
        Recipe recipe = BuildRecipe(portions: 4);

        MacroNutrients total = MacroCalculator.RecipeTotal(recipe);

        Assert.Equal(new MacroNutrients(450m, 24m, 11m, 46m), total);
    }

    [Fact]
    public void RecipePerServing_DividesTotalByPortions()
    {
        Recipe recipe = BuildRecipe(portions: 4);

        MacroNutrients perServing = MacroCalculator.RecipePerServing(recipe);

        Assert.Equal(new MacroNutrients(112.5m, 6m, 2.75m, 11.5m), perServing);
    }

    [Fact]
    public void ForMealEntry_ScalesPerServingBySelectedPortions()
    {
        Recipe recipe = BuildRecipe(portions: 4);
        var entry = new MealPlanEntry(Guid.NewGuid(), new DateOnly(2026, 5, 28), recipe.Id, MealType.Lunch, portions: 2);

        MacroNutrients result = MacroCalculator.ForMealEntry(entry, recipe);

        Assert.Equal(new MacroNutrients(225m, 12m, 5.5m, 23m), result);
    }

    [Fact]
    public void DayTotal_SumsMultipleMealEntries()
    {
        Recipe recipe = BuildRecipe(portions: 4);
        var breakfast = new MealPlanEntry(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 28),
            recipe.Id,
            MealType.Breakfast,
            portions: 1);
        var lunch = new MealPlanEntry(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 28),
            recipe.Id,
            MealType.Lunch,
            portions: 2);

        MacroNutrients result = MacroCalculator.DayTotal([(breakfast, recipe), (lunch, recipe)]);

        Assert.Equal(new MacroNutrients(337.5m, 18m, 8.25m, 34.5m), result);
    }

    [Fact]
    public void DayTotal_ReturnsZero_ForEmptyInput()
    {
        MacroNutrients result = MacroCalculator.DayTotal([]);

        Assert.Equal(MacroNutrients.Zero, result);
    }

    [Fact]
    public void RecipeTotal_UsesIngredientSnapshots_NotCurrentProductValues()
    {
        var productId = Guid.NewGuid();
        var sourceProductAfterEdit = new Product(productId, "Edited oats", new MacroNutrients(999m, 99m, 99m, 99m));
        var recipe = new Recipe(Guid.NewGuid(), "Historical", portions: 1);
        recipe.AddIngredient(new RecipeIngredient(
            productId,
            "Original oats",
            new MacroNutrients(200m, 10m, 5m, 20m),
            new GramAmount(100m)));

        MacroNutrients total = MacroCalculator.RecipeTotal(recipe);

        Assert.Equal(new MacroNutrients(200m, 10m, 5m, 20m), total);
        Assert.NotEqual(sourceProductAfterEdit.Per100Grams, total);
    }

    [Fact]
    public void Calculation_IsRepeatable_ForTheSameInputs()
    {
        Recipe recipe = BuildRecipe(portions: 4);

        MacroNutrients first = MacroCalculator.RecipePerServing(recipe);
        MacroNutrients second = MacroCalculator.RecipePerServing(recipe);

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GramAmount_RejectsNonPositiveValues(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GramAmount(value));
    }

    [Fact]
    public void MacroNutrients_RejectsNegativeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MacroNutrients(-1m, 0m, 0m, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MacroNutrients(0m, -1m, 0m, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MacroNutrients(0m, 0m, -1m, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MacroNutrients(0m, 0m, 0m, -1m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Recipe_RejectsNonPositivePortions(int portions)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Recipe(Guid.NewGuid(), "Soup", portions));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MealPlanEntry_RejectsNonPositivePortions(int portions)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new MealPlanEntry(Guid.NewGuid(), new DateOnly(2026, 5, 28), Guid.NewGuid(), MealType.Dinner, portions));
    }

    [Fact]
    public void Recipe_RejectsDuplicateProductIngredients()
    {
        var productId = Guid.NewGuid();
        var recipe = new Recipe(Guid.NewGuid(), "Double", portions: 1);
        recipe.AddIngredient(new RecipeIngredient(
            productId,
            "Skyr",
            new MacroNutrients(63m, 11m, 0.2m, 4m),
            new GramAmount(100m)));

        Assert.Throws<InvalidOperationException>(
            () => recipe.AddIngredient(new RecipeIngredient(
                productId,
                "Skyr",
                new MacroNutrients(63m, 11m, 0.2m, 4m),
                new GramAmount(50m))));
    }

    private static Recipe BuildRecipe(int portions)
    {
        var oats = new Product(Guid.NewGuid(), "Oats", new MacroNutrients(200m, 10m, 5m, 20m));
        var milk = new Product(Guid.NewGuid(), "Milk", new MacroNutrients(100m, 8m, 2m, 12m));

        var recipe = new Recipe(Guid.NewGuid(), "Porridge", portions);
        recipe.AddIngredient(new RecipeIngredient(oats.Id, oats.Name, oats.Per100Grams, new GramAmount(200m)));
        recipe.AddIngredient(new RecipeIngredient(milk.Id, milk.Name, milk.Per100Grams, new GramAmount(50m)));

        return recipe;
    }
}
