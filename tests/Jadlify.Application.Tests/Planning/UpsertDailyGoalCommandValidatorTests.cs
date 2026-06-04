using Jadlify.Application.Planning.DailyGoals.UpsertDailyGoal;

namespace Jadlify.Application.Tests.Planning;

public class UpsertDailyGoalCommandValidatorTests
{
    private readonly UpsertDailyGoalCommandValidator _validator = new();

    private static UpsertDailyGoalCommand Valid(
        decimal calories = 2200m,
        decimal protein = 160m,
        decimal fat = 70m,
        decimal carbohydrates = 210m) =>
        new(calories, protein, fat, carbohydrates);

    [Fact]
    public void Accepts_RealisticGoal()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    [Fact]
    public void Accepts_ZeroMacroGrams()
    {
        Assert.True(_validator.Validate(Valid(protein: 0m, fat: 0m, carbohydrates: 0m)).IsValid);
    }

    [Fact]
    public void Rejects_ZeroCalories()
    {
        Assert.False(_validator.Validate(Valid(calories: 0m)).IsValid);
    }

    [Fact]
    public void Rejects_NegativeCalories()
    {
        Assert.False(_validator.Validate(Valid(calories: -1m)).IsValid);
    }

    [Fact]
    public void Rejects_NegativeProtein()
    {
        Assert.False(_validator.Validate(Valid(protein: -1m)).IsValid);
    }

    [Fact]
    public void Rejects_UnrealisticCalories()
    {
        Assert.False(_validator.Validate(Valid(calories: 100_000m)).IsValid);
    }

    [Fact]
    public void Rejects_UnrealisticCarbohydrates()
    {
        Assert.False(_validator.Validate(Valid(carbohydrates: 100_000m)).IsValid);
    }
}
