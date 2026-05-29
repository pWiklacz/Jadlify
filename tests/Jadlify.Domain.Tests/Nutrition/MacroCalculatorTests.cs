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
        (Recipe recipe, IReadOnlyDictionary<Guid, Product> products) = BuildRecipe(portions: 4);

        MacroNutrients total = MacroCalculator.RecipeTotal(recipe, products);

        Assert.Equal(new MacroNutrients(450m, 24m, 11m, 46m), total);
    }

    [Fact]
    public void RecipePerServing_DividesTotalByPortions()
    {
        (Recipe recipe, IReadOnlyDictionary<Guid, Product> products) = BuildRecipe(portions: 4);

        MacroNutrients perServing = MacroCalculator.RecipePerServing(recipe, products);

        Assert.Equal(new MacroNutrients(112.5m, 6m, 2.75m, 11.5m), perServing);
    }

    [Fact]
    public void ForMealEntry_ScalesPerServingBySelectedPortions()
    {
        (Recipe recipe, IReadOnlyDictionary<Guid, Product> products) = BuildRecipe(portions: 4);
        var entry = new MealPlanEntry(Guid.NewGuid(), new DateOnly(2026, 5, 28), recipe.Id, MealType.Lunch, portions: 2);

        MacroNutrients result = MacroCalculator.ForMealEntry(entry, recipe, products);

        Assert.Equal(new MacroNutrients(225m, 12m, 5.5m, 23m), result);
    }

    [Fact]
    public void RecipeTotal_Throws_WhenIngredientProductIsMissing()
    {
        var recipe = new Recipe(Guid.NewGuid(), "Mystery", portions: 1);
        recipe.AddIngredient(new RecipeIngredient(Guid.NewGuid(), new GramAmount(100m)));

        Assert.Throws<InvalidOperationException>(
            () => MacroCalculator.RecipeTotal(recipe, new Dictionary<Guid, Product>()));
    }

    [Fact]
    public void Calculation_IsRepeatable_ForTheSameInputs()
    {
        (Recipe recipe, IReadOnlyDictionary<Guid, Product> products) = BuildRecipe(portions: 4);

        MacroNutrients first = MacroCalculator.RecipePerServing(recipe, products);
        MacroNutrients second = MacroCalculator.RecipePerServing(recipe, products);

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
        recipe.AddIngredient(new RecipeIngredient(productId, new GramAmount(100m)));

        Assert.Throws<InvalidOperationException>(
            () => recipe.AddIngredient(new RecipeIngredient(productId, new GramAmount(50m))));
    }

    private static (Recipe Recipe, IReadOnlyDictionary<Guid, Product> Products) BuildRecipe(int portions)
    {
        var oats = new Product(Guid.NewGuid(), "Oats", new MacroNutrients(200m, 10m, 5m, 20m));
        var milk = new Product(Guid.NewGuid(), "Milk", new MacroNutrients(100m, 8m, 2m, 12m));

        var recipe = new Recipe(Guid.NewGuid(), "Porridge", portions);
        recipe.AddIngredient(new RecipeIngredient(oats.Id, new GramAmount(200m)));
        recipe.AddIngredient(new RecipeIngredient(milk.Id, new GramAmount(50m)));

        var products = new Dictionary<Guid, Product>
        {
            [oats.Id] = oats,
            [milk.Id] = milk
        };

        return (recipe, products);
    }
}
