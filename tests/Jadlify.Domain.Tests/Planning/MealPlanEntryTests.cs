using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Products;

namespace Jadlify.Domain.Tests.Planning;

public class MealPlanEntryTests
{
    private static readonly DateOnly Day = new(2026, 6, 2);

    private static PlannedProductSnapshot Snapshot(string name = "Oats") =>
        new(Guid.NewGuid(), name, new MacroNutrients(380m, 13m, 7m, 60m), ProductCategory.GrainsAndBread);

    [Fact]
    public void ForRecipe_SetsRecipeSourceAndLeavesProductFieldsEmpty()
    {
        var id = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        var entry = MealPlanEntry.ForRecipe(id, Day, recipeId, MealType.Breakfast, 1.5m);

        Assert.Equal(MealPlanEntrySource.Recipe, entry.Source);
        Assert.Equal(recipeId, entry.RecipeId);
        Assert.Null(entry.Product);
        Assert.Equal(1.5m, entry.Quantity);
        Assert.Equal(1.5m, entry.RecipePortions);
        Assert.Null(entry.ProductGrams);
    }

    [Fact]
    public void ForProduct_SetsProductSourceAndLeavesRecipeFieldsEmpty()
    {
        PlannedProductSnapshot product = Snapshot();

        var entry = MealPlanEntry.ForProduct(Guid.NewGuid(), Day, product, MealType.Snack, 45m);

        Assert.Equal(MealPlanEntrySource.Product, entry.Source);
        Assert.Null(entry.RecipeId);
        Assert.Same(product, entry.Product);
        Assert.Equal(45m, entry.Quantity);
        Assert.Equal(45m, entry.ProductGrams);
        Assert.Null(entry.RecipePortions);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(1)]
    [InlineData(2.5)]
    [InlineData(100)]
    public void ForRecipe_AcceptsPositiveHalfPortionSteps(decimal portions)
    {
        var entry = MealPlanEntry.ForRecipe(
            Guid.NewGuid(),
            Day,
            Guid.NewGuid(),
            MealType.Lunch,
            portions);

        Assert.Equal(portions, entry.RecipePortions);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    [InlineData(0.25)]
    [InlineData(1.1)]
    [InlineData(1.75)]
    public void ForRecipe_RejectsNonPositiveOrOffStepPortions(decimal portions)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MealPlanEntry.ForRecipe(
            Guid.NewGuid(),
            Day,
            Guid.NewGuid(),
            MealType.Snack,
            portions));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ForProduct_RejectsNonPositiveGrams(decimal grams)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MealPlanEntry.ForProduct(
            Guid.NewGuid(),
            Day,
            Snapshot(),
            MealType.Snack,
            grams));
    }

    [Fact]
    public void ForProduct_AcceptsFractionalGrams()
    {
        // Grams are any positive decimal: the UI's 10 g stepper must not leak into the domain.
        var entry = MealPlanEntry.ForProduct(
            Guid.NewGuid(),
            Day,
            Snapshot(),
            MealType.Snack,
            12.5m);

        Assert.Equal(12.5m, entry.ProductGrams);
    }

    [Fact]
    public void ForProduct_RejectsMissingSnapshot()
    {
        Assert.Throws<ArgumentNullException>(() => MealPlanEntry.ForProduct(
            Guid.NewGuid(),
            Day,
            null!,
            MealType.Snack,
            30m));
    }

    [Fact]
    public void UpdateDetails_ChangesOnlyMealTypeAndQuantity_ForRecipeEntry()
    {
        var id = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var entry = MealPlanEntry.ForRecipe(id, Day, recipeId, MealType.Breakfast, 1m);

        entry.UpdateDetails(MealType.Dinner, 2.5m);

        Assert.Equal(id, entry.Id);
        Assert.Equal(Day, entry.Date);
        Assert.Equal(MealPlanEntrySource.Recipe, entry.Source);
        Assert.Equal(recipeId, entry.RecipeId);
        Assert.Equal(MealType.Dinner, entry.MealType);
        Assert.Equal(2.5m, entry.RecipePortions);
    }

    [Fact]
    public void UpdateDetails_ChangesOnlyMealTypeAndQuantity_ForProductEntry()
    {
        PlannedProductSnapshot product = Snapshot();
        var entry = MealPlanEntry.ForProduct(Guid.NewGuid(), Day, product, MealType.Snack, 30m);

        entry.UpdateDetails(MealType.Lunch, 125m);

        Assert.Equal(Day, entry.Date);
        Assert.Equal(MealPlanEntrySource.Product, entry.Source);
        Assert.Same(product, entry.Product);
        Assert.Equal(MealType.Lunch, entry.MealType);
        Assert.Equal(125m, entry.ProductGrams);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.3)]
    public void UpdateDetails_AppliesPortionRules_ToRecipeEntry(decimal portions)
    {
        var entry = MealPlanEntry.ForRecipe(
            Guid.NewGuid(),
            Day,
            Guid.NewGuid(),
            MealType.Lunch,
            2m);

        Assert.Throws<ArgumentOutOfRangeException>(() => entry.UpdateDetails(MealType.Lunch, portions));
    }

    [Fact]
    public void UpdateDetails_AllowsOffStepQuantity_ForProductEntry()
    {
        // The half-step rule is a recipe-portion rule, not a general quantity rule.
        var entry = MealPlanEntry.ForProduct(
            Guid.NewGuid(),
            Day,
            Snapshot(),
            MealType.Snack,
            30m);

        entry.UpdateDetails(MealType.Snack, 0.3m);

        Assert.Equal(0.3m, entry.ProductGrams);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateDetails_RejectsNonPositiveGrams_ForProductEntry(decimal grams)
    {
        var entry = MealPlanEntry.ForProduct(
            Guid.NewGuid(),
            Day,
            Snapshot(),
            MealType.Snack,
            30m);

        Assert.Throws<ArgumentOutOfRangeException>(() => entry.UpdateDetails(MealType.Snack, grams));
    }

    [Fact]
    public void MoveTo_ChangesDateAndMealType_KeepingIdentityAndSource()
    {
        var id = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var entry = MealPlanEntry.ForRecipe(id, Day, recipeId, MealType.Breakfast, 1.5m);
        DateOnly target = Day.AddDays(3);

        entry.MoveTo(target, MealType.Dinner);

        Assert.Equal(id, entry.Id);
        Assert.Equal(target, entry.Date);
        Assert.Equal(MealType.Dinner, entry.MealType);
        Assert.Equal(MealPlanEntrySource.Recipe, entry.Source);
        Assert.Equal(recipeId, entry.RecipeId);
        Assert.Equal(1.5m, entry.RecipePortions);
    }

    [Fact]
    public void MoveTo_KeepsProductSnapshot()
    {
        PlannedProductSnapshot product = Snapshot();
        var entry = MealPlanEntry.ForProduct(Guid.NewGuid(), Day, product, MealType.Snack, 45m);

        entry.MoveTo(Day.AddDays(-2), MealType.Lunch);

        Assert.Same(product, entry.Product);
        Assert.Equal(45m, entry.ProductGrams);
    }

    [Fact]
    public void MoveTo_ToTheSameDayAndMealType_IsANoOp()
    {
        var entry = MealPlanEntry.ForRecipe(Guid.NewGuid(), Day, Guid.NewGuid(), MealType.Lunch, 2m);

        entry.MoveTo(Day, MealType.Lunch);

        Assert.Equal(Day, entry.Date);
        Assert.Equal(MealType.Lunch, entry.MealType);
    }

    [Fact]
    public void CopyTo_ClonesRecipeEntryUnderNewIdAndDate_LeavingSourceAlone()
    {
        var sourceId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var entry = MealPlanEntry.ForRecipe(sourceId, Day, recipeId, MealType.Dinner, 2.5m);
        var copyId = Guid.NewGuid();
        DateOnly target = Day.AddDays(7);

        MealPlanEntry copy = entry.CopyTo(copyId, target);

        Assert.Equal(copyId, copy.Id);
        Assert.Equal(target, copy.Date);
        Assert.Equal(MealType.Dinner, copy.MealType);
        Assert.Equal(MealPlanEntrySource.Recipe, copy.Source);
        // Both entries keep pointing at the live recipe, so renaming it renames both.
        Assert.Equal(recipeId, copy.RecipeId);
        Assert.Equal(2.5m, copy.RecipePortions);

        Assert.Equal(sourceId, entry.Id);
        Assert.Equal(Day, entry.Date);
    }

    [Fact]
    public void CopyTo_GivesProductEntryItsOwnSnapshotInstance()
    {
        PlannedProductSnapshot product = Snapshot("Rolled oats");
        var entry = MealPlanEntry.ForProduct(Guid.NewGuid(), Day, product, MealType.Snack, 60m);

        MealPlanEntry copy = entry.CopyTo(Guid.NewGuid(), Day.AddDays(1));

        // The snapshot is an owned value: sharing one instance between two entries would make
        // it belong to two owners at once, which persistence rejects.
        Assert.NotNull(copy.Product);
        Assert.NotSame(product, copy.Product);
        Assert.NotSame(product.Per100Grams, copy.Product!.Per100Grams);
        Assert.Equal(product.ProductId, copy.Product.ProductId);
        Assert.Equal("Rolled oats", copy.Product.Name);
        Assert.Equal(ProductCategory.GrainsAndBread, copy.Product.Category);
        Assert.Equal(product.Per100Grams, copy.Product.Per100Grams);
        Assert.Equal(60m, copy.ProductGrams);
    }

    [Fact]
    public void CopyTo_ToTheSourcesOwnDate_DuplicatesTheMeal()
    {
        var entry = MealPlanEntry.ForRecipe(Guid.NewGuid(), Day, Guid.NewGuid(), MealType.Lunch, 1m);

        MealPlanEntry copy = entry.CopyTo(Guid.NewGuid(), Day);

        Assert.NotEqual(entry.Id, copy.Id);
        Assert.Equal(Day, copy.Date);
    }
}
