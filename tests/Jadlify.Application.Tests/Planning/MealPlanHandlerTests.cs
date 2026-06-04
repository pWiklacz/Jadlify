using Jadlify.Application.Planning;
using Jadlify.Application.Planning.MealPlans.AddMealPlanEntry;
using Jadlify.Application.Planning.MealPlans.DeleteMealPlanEntry;
using Jadlify.Application.Planning.MealPlans.ListMealPlanEntries;
using Jadlify.Application.Planning.MealPlans.UpdateMealPlanEntry;
using Jadlify.Application.Tests.Recipes;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Planning;

public class MealPlanHandlerTests
{
    private static readonly DateOnly Day = new(2026, 6, 2);

    [Fact]
    public async Task AddEntry_AddsEntry_WhenRecipeExistsForUser()
    {
        var recipe = new Recipe(Guid.NewGuid(), "Porridge", 2);
        var mealPlans = new FakeMealPlanRepository();
        var handler = new AddMealPlanEntryCommandHandler(mealPlans, new FakeRecipeRepository(recipe));

        Result<Guid> result = await handler.HandleAsync(
            new AddMealPlanEntryCommand(Day, recipe.Id, MealType.Breakfast, 2),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        MealPlanEntry entry = Assert.Single(mealPlans.Entries);
        Assert.Equal(result.Value, entry.Id);
        Assert.Equal(Day, entry.Date);
        Assert.Equal(recipe.Id, entry.RecipeId);
        Assert.Equal(MealType.Breakfast, entry.MealType);
        Assert.Equal(2, entry.Portions);
    }

    [Fact]
    public async Task AddEntry_ReturnsValidationFailure_WhenRecipeMissingOrCrossUser()
    {
        // The owner-scoped recipe repository returns null for both a missing recipe and
        // another user's recipe, so this single path covers cross-user rejection too.
        var mealPlans = new FakeMealPlanRepository();
        var handler = new AddMealPlanEntryCommandHandler(mealPlans, new FakeRecipeRepository());

        Result<Guid> result = await handler.HandleAsync(
            new AddMealPlanEntryCommand(Day, Guid.NewGuid(), MealType.Lunch, 1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Empty(mealPlans.Entries);
    }

    [Fact]
    public async Task AddEntry_AllowsDuplicateEntries()
    {
        var recipe = new Recipe(Guid.NewGuid(), "Porridge", 2);
        var mealPlans = new FakeMealPlanRepository();
        var handler = new AddMealPlanEntryCommandHandler(mealPlans, new FakeRecipeRepository(recipe));
        var command = new AddMealPlanEntryCommand(Day, recipe.Id, MealType.Breakfast, 1);

        Result<Guid> first = await handler.HandleAsync(command, CancellationToken.None);
        Result<Guid> second = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(2, mealPlans.Entries.Count);
        Assert.NotEqual(first.Value, second.Value);
    }

    [Fact]
    public async Task ListEntries_ReturnsEntriesForDate_WithCurrentRecipeName()
    {
        var recipe = new Recipe(Guid.NewGuid(), "Porridge", 2);
        var entry = new MealPlanEntry(Guid.NewGuid(), Day, recipe.Id, MealType.Breakfast, 1);
        var otherDayEntry = new MealPlanEntry(Guid.NewGuid(), Day.AddDays(1), recipe.Id, MealType.Dinner, 1);
        var mealPlans = new FakeMealPlanRepository(entry, otherDayEntry);
        var handler = new ListMealPlanEntriesQueryHandler(mealPlans, new FakeRecipeRepository(recipe));

        Result<IReadOnlyList<MealPlanEntryDto>> result =
            await handler.HandleAsync(new ListMealPlanEntriesQuery(Day), CancellationToken.None);

        Assert.True(result.IsSuccess);
        MealPlanEntryDto dto = Assert.Single(result.Value);
        Assert.Equal(entry.Id, dto.Id);
        Assert.Equal(recipe.Id, dto.RecipeId);
        Assert.Equal("Porridge", dto.RecipeName);
        Assert.Equal(MealType.Breakfast, dto.MealType);
    }

    [Fact]
    public async Task ListEntries_ReturnsEmpty_WhenNoEntriesForDate()
    {
        var handler = new ListMealPlanEntriesQueryHandler(
            new FakeMealPlanRepository(),
            new FakeRecipeRepository());

        Result<IReadOnlyList<MealPlanEntryDto>> result =
            await handler.HandleAsync(new ListMealPlanEntriesQuery(Day), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task UpdateEntry_ChangesOnlyMealTypeAndPortions()
    {
        var recipeId = Guid.NewGuid();
        var entry = new MealPlanEntry(Guid.NewGuid(), Day, recipeId, MealType.Breakfast, 1);
        var mealPlans = new FakeMealPlanRepository(entry);
        var handler = new UpdateMealPlanEntryCommandHandler(mealPlans);

        Result result = await handler.HandleAsync(
            new UpdateMealPlanEntryCommand(entry.Id, MealType.Dinner, 3),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, mealPlans.UpdateCount);
        MealPlanEntry updated = Assert.Single(mealPlans.Entries);
        Assert.Equal(Day, updated.Date);
        Assert.Equal(recipeId, updated.RecipeId);
        Assert.Equal(MealType.Dinner, updated.MealType);
        Assert.Equal(3, updated.Portions);
    }

    [Fact]
    public async Task UpdateEntry_ReturnsNotFound_WhenEntryMissingOrCrossUser()
    {
        var mealPlans = new FakeMealPlanRepository();
        var handler = new UpdateMealPlanEntryCommandHandler(mealPlans);

        Result result = await handler.HandleAsync(
            new UpdateMealPlanEntryCommand(Guid.NewGuid(), MealType.Dinner, 2),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal(0, mealPlans.UpdateCount);
    }

    [Fact]
    public async Task DeleteEntry_DelegatesToRepository()
    {
        var entry = new MealPlanEntry(Guid.NewGuid(), Day, Guid.NewGuid(), MealType.Breakfast, 1);
        var mealPlans = new FakeMealPlanRepository(entry);
        var handler = new DeleteMealPlanEntryCommandHandler(mealPlans);

        Result result = await handler.HandleAsync(
            new DeleteMealPlanEntryCommand(entry.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(entry.Id, Assert.Single(mealPlans.DeletedIds));
        Assert.Empty(mealPlans.Entries);
    }
}
