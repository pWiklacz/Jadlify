using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Products;
using Jadlify.Domain.Recipes;
using Jadlify.Domain.Shopping;

namespace Jadlify.Domain.Tests.Shopping;

public class ShoppingListProjectionTests
{
    private static readonly DateOnly Day = new(2026, 6, 6);

    [Fact]
    public void ForMealEntries_ProductEntry_CarriesCategoryAndOneContribution()
    {
        var productId = Guid.NewGuid();
        var entry = MealPlanEntry.ForProduct(
            Guid.NewGuid(),
            Day,
            new PlannedProductSnapshot(productId, "Oats", new MacroNutrients(380m, 13m, 7m, 60m), ProductCategory.GrainsAndBread),
            MealType.Snack,
            grams: 45m);

        ShoppingProjectionItem item = Assert.Single(ShoppingListCalculator.ForMealEntries([(entry, null)]));

        Assert.Equal(ProductCategory.GrainsAndBread, item.Category);
        ShoppingProjectionContribution contribution = Assert.Single(item.Contributions);
        Assert.Equal(Day, contribution.Date);
        Assert.Equal(MealType.Snack, contribution.MealType);
        Assert.Equal("Oats", contribution.SourceLabel);
        Assert.Equal(45m, contribution.Grams);
    }

    [Fact]
    public void ForMealEntries_RecipeIngredient_HasNullCategory_AndRecipeLabelledContribution()
    {
        var productId = Guid.NewGuid();
        Recipe recipe = BuildRecipe("Porridge", portions: 2, Ingredient(productId, "Oats", 200m));
        var entry = MealPlanEntry.ForRecipe(Guid.NewGuid(), Day, recipe.Id, MealType.Breakfast, portions: 1);

        ShoppingProjectionItem item = Assert.Single(ShoppingListCalculator.ForMealEntries([(entry, recipe)]));

        Assert.Null(item.Category);
        ShoppingProjectionContribution contribution = Assert.Single(item.Contributions);
        Assert.Equal("Porridge", contribution.SourceLabel);
        // Oracle: 200 g across 2 portions is 100 g per portion; one portion is 100 g.
        Assert.Equal(100m, contribution.Grams);
    }

    [Fact]
    public void ForMealEntries_ProductPlannedBothWays_MergesContributions_AndTakesCategoryFromProductEntry()
    {
        var sharedProductId = Guid.NewGuid();
        Recipe porridge = BuildRecipe("Porridge", portions: 4, Ingredient(sharedProductId, "Oats", 400m));
        var fromRecipe = MealPlanEntry.ForRecipe(Guid.NewGuid(), Day, porridge.Id, MealType.Breakfast, portions: 2);
        var direct = MealPlanEntry.ForProduct(
            Guid.NewGuid(),
            Day,
            new PlannedProductSnapshot(sharedProductId, "Oats", new MacroNutrients(380m, 13m, 7m, 60m), ProductCategory.GrainsAndBread),
            MealType.Snack,
            grams: 45m);

        ShoppingProjectionItem item =
            Assert.Single(ShoppingListCalculator.ForMealEntries([(fromRecipe, porridge), (direct, null)]));

        // One bucket, both contributions, category taken from the direct entry.
        Assert.Equal(245m, item.Grams);
        Assert.Equal(ProductCategory.GrainsAndBread, item.Category);
        Assert.Equal(2, item.Contributions.Count);
        Assert.Contains(item.Contributions, c => c.MealType == MealType.Breakfast && c.Grams == 200m);
        Assert.Contains(item.Contributions, c => c.MealType == MealType.Snack && c.Grams == 45m);
    }

    [Fact]
    public void ComputeFingerprint_IsStable_ForEquivalentProjections()
    {
        var productId = Guid.NewGuid();
        ShoppingProjectionItem first = Item(productId, "Oats", 100m, Contribution(MealType.Breakfast, "Porridge", 100m));
        ShoppingProjectionItem second = Item(productId, "Oats", 100m, Contribution(MealType.Breakfast, "Porridge", 100m));

        Assert.Equal(
            ShoppingListCalculator.ComputeFingerprint([first]),
            ShoppingListCalculator.ComputeFingerprint([second]));
    }

    [Fact]
    public void ComputeFingerprint_Differs_WhenSourceCompositionChanges_EvenIfTotalIsEqual()
    {
        var productId = Guid.NewGuid();
        ShoppingProjectionItem viaBreakfast =
            Item(productId, "Oats", 100m, Contribution(MealType.Breakfast, "Porridge", 100m));
        ShoppingProjectionItem viaLunch =
            Item(productId, "Oats", 100m, Contribution(MealType.Lunch, "Overnight oats", 100m));

        // Same product, same total, different meal composition: a source-only change is still a change.
        Assert.NotEqual(
            ShoppingListCalculator.ComputeFingerprint([viaBreakfast]),
            ShoppingListCalculator.ComputeFingerprint([viaLunch]));
    }

    [Fact]
    public void ComputeFingerprint_IsOrderIndependent_AcrossItems()
    {
        var oatsId = Guid.NewGuid();
        var milkId = Guid.NewGuid();
        ShoppingProjectionItem oats = Item(oatsId, "Oats", 100m, Contribution(MealType.Breakfast, "Porridge", 100m));
        ShoppingProjectionItem milk = Item(milkId, "Milk", 200m, Contribution(MealType.Breakfast, "Porridge", 200m));

        Assert.Equal(
            ShoppingListCalculator.ComputeFingerprint([oats, milk]),
            ShoppingListCalculator.ComputeFingerprint([milk, oats]));
    }

    private static ShoppingProjectionItem Item(
        Guid productId,
        string name,
        decimal grams,
        params ShoppingProjectionContribution[] contributions) =>
        new(productId, name, null, grams, contributions);

    private static ShoppingProjectionContribution Contribution(MealType mealType, string label, decimal grams) =>
        new(Day, mealType, label, grams);

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
