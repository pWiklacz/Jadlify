using Jadlify.Application.Identity;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Recipes;
using Jadlify.Infrastructure.Persistence;
using Jadlify.Infrastructure.Persistence.Repositories;

namespace Jadlify.Infrastructure.Tests.Persistence;

public class MealPlanRepositoryTests
{
    private static readonly ApplicationUserId OwnerId = new("user-owner");
    private static readonly ApplicationUserId OtherId = new("user-other");

    [Fact]
    public async Task ListByDateAsync_ReturnsOnlyCurrentUsersEntries()
    {
        using SqliteTestDatabase database = new();
        DateOnly date = new(2026, 5, 28);
        var ownerRecipeId = Guid.NewGuid();
        var otherRecipeId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository ownerRecipes = new(context, new TestCurrentUser(OwnerId));
            await ownerRecipes.AddAsync(new Recipe(ownerRecipeId, "Porridge", 2));
            RecipeRepository otherRecipes = new(context, new TestCurrentUser(OtherId));
            await otherRecipes.AddAsync(new Recipe(otherRecipeId, "Salad", 1));

            MealPlanRepository ownerPlans = new(context, new TestCurrentUser(OwnerId));
            await ownerPlans.AddAsync(
                new MealPlanEntry(Guid.NewGuid(), date, ownerRecipeId, MealType.Breakfast, 1));
            MealPlanRepository otherPlans = new(context, new TestCurrentUser(OtherId));
            await otherPlans.AddAsync(
                new MealPlanEntry(Guid.NewGuid(), date, otherRecipeId, MealType.Lunch, 1));
        }

        IReadOnlyList<MealPlanEntry> entries;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository repository = new(context, new TestCurrentUser(OwnerId));
            entries = await repository.ListByDateAsync(date);
        }

        Assert.Single(entries);
        Assert.Equal(ownerRecipeId, entries[0].RecipeId);
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotReturnAnotherUsersEntry()
    {
        using SqliteTestDatabase database = new();
        DateOnly date = new(2026, 5, 28);
        var recipeId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            await recipes.AddAsync(new Recipe(recipeId, "Porridge", 2));

            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            await plans.AddAsync(new MealPlanEntry(entryId, date, recipeId, MealType.Breakfast, 1));
        }

        MealPlanEntry? found;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository plans = new(context, new TestCurrentUser(OtherId));
            found = await plans.GetByIdAsync(entryId);
        }

        Assert.Null(found);
    }

    [Fact]
    public async Task DeleteAsync_DoesNotRemoveAnotherUsersEntry()
    {
        using SqliteTestDatabase database = new();
        DateOnly date = new(2026, 5, 28);
        var recipeId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            await recipes.AddAsync(new Recipe(recipeId, "Porridge", 2));

            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            await plans.AddAsync(new MealPlanEntry(entryId, date, recipeId, MealType.Breakfast, 1));
        }

        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository plans = new(context, new TestCurrentUser(OtherId));
            await plans.DeleteAsync(entryId);
        }

        MealPlanEntry? remaining;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            remaining = await plans.GetByIdAsync(entryId);
        }

        Assert.NotNull(remaining);
    }

    [Fact]
    public async Task UpdateAsync_ChangesMealTypeAndPortions()
    {
        using SqliteTestDatabase database = new();
        DateOnly date = new(2026, 5, 28);
        var recipeId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            await recipes.AddAsync(new Recipe(recipeId, "Porridge", 2));

            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            await plans.AddAsync(new MealPlanEntry(entryId, date, recipeId, MealType.Breakfast, 1));
        }

        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            MealPlanEntry entry = (await plans.GetByIdAsync(entryId))!;
            entry.UpdateDetails(MealType.Dinner, 3);
            await plans.UpdateAsync(entry);
        }

        MealPlanEntry? reloaded;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            reloaded = await plans.GetByIdAsync(entryId);
        }

        Assert.NotNull(reloaded);
        Assert.Equal(MealType.Dinner, reloaded!.MealType);
        Assert.Equal(3, reloaded.Portions);
        // Date and recipe stay put: only meal type and portions are editable.
        Assert.Equal(date, reloaded.Date);
        Assert.Equal(recipeId, reloaded.RecipeId);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotMutateAnotherUsersEntry()
    {
        using SqliteTestDatabase database = new();
        DateOnly date = new(2026, 5, 28);
        var recipeId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            await recipes.AddAsync(new Recipe(recipeId, "Porridge", 2));

            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            await plans.AddAsync(new MealPlanEntry(entryId, date, recipeId, MealType.Breakfast, 1));
        }

        await using (JadlifyDbContext context = database.CreateContext())
        {
            // Another user attempts the update; the owner-scoped repository no-ops.
            MealPlanRepository plans = new(context, new TestCurrentUser(OtherId));
            await plans.UpdateAsync(new MealPlanEntry(entryId, date, recipeId, MealType.Dinner, 9));
        }

        MealPlanEntry? reloaded;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            reloaded = await plans.GetByIdAsync(entryId);
        }

        Assert.NotNull(reloaded);
        Assert.Equal(MealType.Breakfast, reloaded!.MealType);
        Assert.Equal(1, reloaded.Portions);
    }
}
