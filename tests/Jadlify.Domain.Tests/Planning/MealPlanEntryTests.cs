using Jadlify.Domain.Planning;

namespace Jadlify.Domain.Tests.Planning;

public class MealPlanEntryTests
{
    [Fact]
    public void UpdateDetails_ChangesOnlyMealTypeAndPortions()
    {
        var id = Guid.NewGuid();
        var date = new DateOnly(2026, 6, 2);
        var recipeId = Guid.NewGuid();
        var entry = new MealPlanEntry(id, date, recipeId, MealType.Breakfast, 1);

        entry.UpdateDetails(MealType.Dinner, 3);

        Assert.Equal(id, entry.Id);
        Assert.Equal(date, entry.Date);
        Assert.Equal(recipeId, entry.RecipeId);
        Assert.Equal(MealType.Dinner, entry.MealType);
        Assert.Equal(3, entry.Portions);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateDetails_RejectsNonPositivePortions(int portions)
    {
        var entry = new MealPlanEntry(
            Guid.NewGuid(),
            new DateOnly(2026, 6, 2),
            Guid.NewGuid(),
            MealType.Lunch,
            2);

        Assert.Throws<ArgumentOutOfRangeException>(() => entry.UpdateDetails(MealType.Lunch, portions));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_RejectsNonPositivePortions(int portions)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MealPlanEntry(
            Guid.NewGuid(),
            new DateOnly(2026, 6, 2),
            Guid.NewGuid(),
            MealType.Snack,
            portions));
    }
}
