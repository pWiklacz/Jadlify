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
        var entry = MealPlanEntry.ForRecipe(Guid.NewGuid(), new DateOnly(2026, 5, 28), recipe.Id, MealType.Lunch, portions: 2);

        MacroNutrients result = MacroCalculator.ForMealEntry(entry, recipe);

        Assert.Equal(new MacroNutrients(225m, 12m, 5.5m, 23m), result);
    }

    [Fact]
    public void DayTotal_SumsMultipleMealEntries()
    {
        Recipe recipe = BuildRecipe(portions: 4);
        var breakfast = MealPlanEntry.ForRecipe(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 28),
            recipe.Id,
            MealType.Breakfast,
            portions: 1);
        var lunch = MealPlanEntry.ForRecipe(
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
    public void ForMealEntry_ScalesPerServingByHalfPortions()
    {
        // Oracle: total 450/24/11/46 over 4 portions = 112.5/6/2.75/11.5 per serving;
        // half a serving halves each value exactly, with no intermediate rounding.
        Recipe recipe = BuildRecipe(portions: 4);
        var entry = MealPlanEntry.ForRecipe(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 28),
            recipe.Id,
            MealType.Snack,
            portions: 0.5m);

        MacroNutrients result = MacroCalculator.ForMealEntry(entry, recipe);

        Assert.Equal(new MacroNutrients(56.25m, 3m, 1.375m, 5.75m), result);
    }

    [Fact]
    public void ForMealEntry_ScalesProductSnapshotByGrams()
    {
        // Oracle: 380/13/7/60 per 100 g at 45 g = 171/5.85/3.15/27.
        var entry = MealPlanEntry.ForProduct(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 28),
            OatsSnapshot(),
            MealType.Snack,
            grams: 45m);

        MacroNutrients result = MacroCalculator.ForMealEntry(entry, recipe: null);

        Assert.Equal(new MacroNutrients(171m, 5.85m, 3.15m, 27m), result);
    }

    [Fact]
    public void ForMealEntry_IgnoresRecipe_ForAProductEntry()
    {
        // A product entry is self-contained; passing a recipe alongside must not change it.
        Recipe recipe = BuildRecipe(portions: 4);
        var entry = MealPlanEntry.ForProduct(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 28),
            OatsSnapshot(),
            MealType.Snack,
            grams: 100m);

        Assert.Equal(
            MacroCalculator.ForMealEntry(entry, recipe: null),
            MacroCalculator.ForMealEntry(entry, recipe));
    }

    [Fact]
    public void DayTotal_SumsMixedRecipeAndProductEntries()
    {
        // Oracle: 112.5 kcal for one serving + 190 kcal for 50 g of a 380 kcal/100 g product.
        Recipe recipe = BuildRecipe(portions: 4);
        var lunch = MealPlanEntry.ForRecipe(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 28),
            recipe.Id,
            MealType.Lunch,
            portions: 1m);
        var snack = MealPlanEntry.ForProduct(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 28),
            OatsSnapshot(),
            MealType.Snack,
            grams: 50m);

        MacroNutrients result = MacroCalculator.DayTotal([(lunch, recipe), (snack, null)]);

        Assert.Equal(new MacroNutrients(302.5m, 12.5m, 6.25m, 41.5m), result);
    }

    [Fact]
    public void ForMealEntry_Throws_WhenARecipeEntryHasNoRecipe()
    {
        // A missing recipe is a caller-side resolution failure, not a silent zero here.
        var entry = MealPlanEntry.ForRecipe(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 28),
            Guid.NewGuid(),
            MealType.Lunch,
            portions: 1m);

        Assert.Throws<ArgumentNullException>(() => MacroCalculator.ForMealEntry(entry, recipe: null));
    }

    private static PlannedProductSnapshot OatsSnapshot() =>
        new(
            Guid.NewGuid(),
            "Oats",
            new MacroNutrients(380m, 13m, 7m, 60m),
            ProductCategory.GrainsAndBread);

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
            () => MealPlanEntry.ForRecipe(Guid.NewGuid(), new DateOnly(2026, 5, 28), Guid.NewGuid(), MealType.Dinner, portions));
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

    [Fact]
    public void RecipeTotal_WithFractionalGrams_MatchesIndependentOracle()
    {
        // Two ingredients with non-round whole-recipe gram amounts in a 3-portion recipe.
        var flour = new Product(Guid.NewGuid(), "Flour", new MacroNutrients(200m, 10m, 5m, 20m));
        var oil = new Product(Guid.NewGuid(), "Oil", new MacroNutrients(150m, 6m, 3m, 9m));

        var recipe = new Recipe(Guid.NewGuid(), "Fractional", portions: 3);
        recipe.AddIngredient(new RecipeIngredient(flour.Id, flour.Name, flour.Per100Grams, new GramAmount(33.3m)));
        recipe.AddIngredient(new RecipeIngredient(oil.Id, oil.Name, oil.Per100Grams, new GramAmount(66.7m)));

        MacroNutrients total = MacroCalculator.RecipeTotal(recipe);

        // Oracle (computed by hand from first principles: per-100g × grams ÷ 100, then summed):
        //   Flour: 33.3 g of (200,10,5,20)/100g (×0.333) = (66.60, 3.330, 1.665, 6.660)
        //   Oil:   66.7 g of (150, 6,3, 9)/100g (×0.667) = (100.05, 4.002, 2.001, 6.003)
        //   total                                        = (166.65, 7.332, 3.666, 12.663)
        Assert.Equal(new MacroNutrients(166.65m, 7.332m, 3.666m, 12.663m), total);
    }

    [Fact]
    public void RecipePerServing_WithThreePortions_ProducesRepeatingDecimal()
    {
        // Single 100 g ingredient at 100 kcal / 100 g → recipe total = exactly 100 kcal.
        var rice = new Product(Guid.NewGuid(), "Rice", new MacroNutrients(100m, 9m, 1m, 80m));
        var recipe = new Recipe(Guid.NewGuid(), "Solo", portions: 3);
        recipe.AddIngredient(new RecipeIngredient(rice.Id, rice.Name, rice.Per100Grams, new GramAmount(100m)));

        MacroNutrients perServing = MacroCalculator.RecipePerServing(recipe);

        // Oracle: 100 kcal ÷ 3 portions = 33.3333… kcal — a non-terminating decimal.
        // Independently, 100/3 lies strictly between 33.3333333333 and 33.3333333334.
        // The domain contract is full-precision decimal with NO rounding: a currency-style
        // 2-dp rounding would collapse the value to 33.33, which is outside that band.
        Assert.InRange(perServing.Calories, 33.3333333333m, 33.3333333334m);
        Assert.NotEqual(33.33m, perServing.Calories);
        // Proven not rounded: the value carries more precision than its own 2-dp rounding.
        Assert.NotEqual(Math.Round(perServing.Calories, 2), perServing.Calories);
    }

    [Fact]
    public void ForMealEntry_WhenEntryPortionsExceedRecipePortions_ScalesUp()
    {
        // A 4-portion recipe; the diner plans 10 portions (cooked the recipe 2.5×).
        var oats = new Product(Guid.NewGuid(), "Oats", new MacroNutrients(200m, 10m, 5m, 20m));
        var milk = new Product(Guid.NewGuid(), "Milk", new MacroNutrients(100m, 8m, 2m, 12m));
        var recipe = new Recipe(Guid.NewGuid(), "Porridge", portions: 4);
        recipe.AddIngredient(new RecipeIngredient(oats.Id, oats.Name, oats.Per100Grams, new GramAmount(200m)));
        recipe.AddIngredient(new RecipeIngredient(milk.Id, milk.Name, milk.Per100Grams, new GramAmount(100m)));

        var entry = MealPlanEntry.ForRecipe(Guid.NewGuid(), new DateOnly(2026, 5, 28), recipe.Id, MealType.Lunch, portions: 10);

        MacroNutrients result = MacroCalculator.ForMealEntry(entry, recipe);

        // Oracle (per-100g × grams ÷ 100, summed, ÷ recipe portions, × entry portions):
        //   Oats: 200 g of (200,10,5,20)/100g (×2) = (400,20,10,40)
        //   Milk: 100 g of (100, 8,2,12)/100g (×1) = (100, 8, 2,12)
        //   recipe total                           = (500,28,12,52)
        //   per serving (÷ 4 portions)             = (125, 7, 3,13)
        //   meal entry  (× 10 portions, factor 2.5)= (1250,70,30,130)
        Assert.Equal(new MacroNutrients(1250m, 70m, 30m, 130m), result);
    }

    [Fact]
    public void DayTotal_WithMultipleDifferentRecipes_MatchesIndependentOracle()
    {
        // Recipe A: 4 portions, two ingredients.
        var oats = new Product(Guid.NewGuid(), "Oats", new MacroNutrients(200m, 10m, 5m, 20m));
        var milk = new Product(Guid.NewGuid(), "Milk", new MacroNutrients(100m, 8m, 2m, 12m));
        var recipeA = new Recipe(Guid.NewGuid(), "Porridge", portions: 4);
        recipeA.AddIngredient(new RecipeIngredient(oats.Id, oats.Name, oats.Per100Grams, new GramAmount(200m)));
        recipeA.AddIngredient(new RecipeIngredient(milk.Id, milk.Name, milk.Per100Grams, new GramAmount(50m)));

        // Recipe B: 5 portions, one ingredient — deliberately different from A.
        var chicken = new Product(Guid.NewGuid(), "Chicken", new MacroNutrients(150m, 12m, 4m, 9m));
        var recipeB = new Recipe(Guid.NewGuid(), "Roast", portions: 5);
        recipeB.AddIngredient(new RecipeIngredient(chicken.Id, chicken.Name, chicken.Per100Grams, new GramAmount(250m)));

        var entryA = MealPlanEntry.ForRecipe(Guid.NewGuid(), new DateOnly(2026, 5, 28), recipeA.Id, MealType.Breakfast, portions: 2);
        var entryB = MealPlanEntry.ForRecipe(Guid.NewGuid(), new DateOnly(2026, 5, 28), recipeB.Id, MealType.Dinner, portions: 3);

        MacroNutrients result = MacroCalculator.DayTotal([(entryA, recipeA), (entryB, recipeB)]);

        // Oracle (each entry computed independently, then summed):
        //   Recipe A total: 200 g×(200,10,5,20)/100g + 50 g×(100,8,2,12)/100g
        //                 = (400,20,10,40) + (50,4,1,6) = (450,24,11,46)
        //     per serving (÷4)            = (112.5, 6, 2.75, 11.5)
        //     entry A (× 2 portions)      = (225, 12, 5.5, 23)
        //   Recipe B total: 250 g×(150,12,4,9)/100g (×2.5) = (375, 30, 10, 22.5)
        //     per serving (÷5)            = (75, 6, 2, 4.5)
        //     entry B (× 3 portions)      = (225, 18, 6, 13.5)
        //   day total = entry A + entry B = (450, 30, 11.5, 36.5)
        Assert.Equal(new MacroNutrients(450m, 30m, 11.5m, 36.5m), result);
    }

    [Fact]
    public void WholeRecipeConvention_GramsAreForEntireRecipe_NotPerServing()
    {
        // RecipeIngredient.WholeRecipeAmount is grams used across the WHOLE recipe, not per serving.
        // 400 g of a (400,40,10,20)/100g product in a 4-portion recipe.
        var product = new Product(Guid.NewGuid(), "Beef", new MacroNutrients(400m, 40m, 10m, 20m));
        var recipe = new Recipe(Guid.NewGuid(), "Stew", portions: 4);
        recipe.AddIngredient(new RecipeIngredient(product.Id, product.Name, product.Per100Grams, new GramAmount(400m)));

        MacroNutrients total = MacroCalculator.RecipeTotal(recipe);
        MacroNutrients perServing = MacroCalculator.RecipePerServing(recipe);

        // Oracle: 400 g is for the whole recipe → recipe total = per-100g × (400 ÷ 100) = ×4.
        //   recipe total = (1600, 160, 40, 80)
        //   per serving  = total ÷ 4 portions = (400, 40, 10, 20)
        Assert.Equal(new MacroNutrients(1600m, 160m, 40m, 80m), total);
        Assert.Equal(new MacroNutrients(400m, 40m, 10m, 20m), perServing);

        // If 400 g were misread as PER SERVING, per serving would be 4× larger.
        // Asserting it is NOT that value makes the per-whole-recipe convention explicit.
        Assert.NotEqual(new MacroNutrients(1600m, 160m, 40m, 80m), perServing);
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
