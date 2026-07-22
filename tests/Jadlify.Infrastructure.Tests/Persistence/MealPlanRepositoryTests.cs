using Jadlify.Application.Identity;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Products;
using Jadlify.Domain.Recipes;
using Jadlify.Infrastructure.Persistence;
using Jadlify.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Jadlify.Infrastructure.Tests.Persistence;

public class MealPlanRepositoryTests
{
    private static readonly ApplicationUserId OwnerId = new("user-owner");
    private static readonly ApplicationUserId OtherId = new("user-other");

    private static PlannedProductSnapshot OatsSnapshot() =>
        new(
            Guid.NewGuid(),
            "Oats",
            new MacroNutrients(380m, 13m, 7m, 60m),
            ProductCategory.GrainsAndBread);

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
                MealPlanEntry.ForRecipe(Guid.NewGuid(), date, ownerRecipeId, MealType.Breakfast, 1));
            MealPlanRepository otherPlans = new(context, new TestCurrentUser(OtherId));
            await otherPlans.AddAsync(
                MealPlanEntry.ForRecipe(Guid.NewGuid(), date, otherRecipeId, MealType.Lunch, 1));
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
            await plans.AddAsync(MealPlanEntry.ForRecipe(entryId, date, recipeId, MealType.Breakfast, 1));
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
            await plans.AddAsync(MealPlanEntry.ForRecipe(entryId, date, recipeId, MealType.Breakfast, 1));
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
            await plans.AddAsync(MealPlanEntry.ForRecipe(entryId, date, recipeId, MealType.Breakfast, 1));
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
        Assert.Equal(3m, reloaded.RecipePortions);
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
            await plans.AddAsync(MealPlanEntry.ForRecipe(entryId, date, recipeId, MealType.Breakfast, 1));
        }

        await using (JadlifyDbContext context = database.CreateContext())
        {
            // Another user attempts the update; the owner-scoped repository no-ops.
            MealPlanRepository plans = new(context, new TestCurrentUser(OtherId));
            await plans.UpdateAsync(MealPlanEntry.ForRecipe(entryId, date, recipeId, MealType.Dinner, 9));
        }

        MealPlanEntry? reloaded;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            reloaded = await plans.GetByIdAsync(entryId);
        }

        Assert.NotNull(reloaded);
        Assert.Equal(MealType.Breakfast, reloaded!.MealType);
        Assert.Equal(1m, reloaded.RecipePortions);
    }

    [Fact]
    public async Task ListUsedRecipeIdsAsync_ReturnsRequestedIdsPlannedByCurrentUserOnly()
    {
        using SqliteTestDatabase database = new();
        DateOnly date = new(2026, 5, 28);
        var plannedId = Guid.NewGuid();
        var unplannedId = Guid.NewGuid();
        var otherUsersPlannedId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository ownerRecipes = new(context, new TestCurrentUser(OwnerId));
            await ownerRecipes.AddAsync(new Recipe(plannedId, "Planned", 1));
            await ownerRecipes.AddAsync(new Recipe(unplannedId, "Unplanned", 1));
            RecipeRepository otherRecipes = new(context, new TestCurrentUser(OtherId));
            await otherRecipes.AddAsync(new Recipe(otherUsersPlannedId, "Theirs", 1));

            MealPlanRepository ownerPlans = new(context, new TestCurrentUser(OwnerId));
            // Two entries for the same recipe: the projection must be distinct.
            await ownerPlans.AddAsync(
                MealPlanEntry.ForRecipe(Guid.NewGuid(), date, plannedId, MealType.Breakfast, 1));
            await ownerPlans.AddAsync(
                MealPlanEntry.ForRecipe(Guid.NewGuid(), date, plannedId, MealType.Dinner, 1));

            MealPlanRepository otherPlans = new(context, new TestCurrentUser(OtherId));
            await otherPlans.AddAsync(
                MealPlanEntry.ForRecipe(Guid.NewGuid(), date, otherUsersPlannedId, MealType.Lunch, 1));
        }

        IReadOnlyCollection<Guid> used;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository repository = new(context, new TestCurrentUser(OwnerId));
            used = await repository.ListUsedRecipeIdsAsync(
                [plannedId, unplannedId, otherUsersPlannedId]);
        }

        Assert.Equal([plannedId], used);
    }

    [Fact]
    public async Task ListUsedRecipeIdsAsync_ReturnsEmpty_WhenNoIdsRequested()
    {
        using SqliteTestDatabase database = new();

        IReadOnlyCollection<Guid> used;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository repository = new(context, new TestCurrentUser(OwnerId));
            used = await repository.ListUsedRecipeIdsAsync([]);
        }

        Assert.Empty(used);
    }

    [Fact]
    public async Task ProductEntry_RoundTripsItsSnapshot()
    {
        using SqliteTestDatabase database = new();
        DateOnly date = new(2026, 5, 28);
        PlannedProductSnapshot snapshot = OatsSnapshot();
        var entryId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            await plans.AddAsync(
                MealPlanEntry.ForProduct(entryId, date, snapshot, MealType.Snack, 45.5m));
        }

        MealPlanEntry? reloaded;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            reloaded = await plans.GetByIdAsync(entryId);
        }

        Assert.NotNull(reloaded);
        Assert.Equal(MealPlanEntrySource.Product, reloaded!.Source);
        Assert.Null(reloaded.RecipeId);
        Assert.Equal(45.5m, reloaded.ProductGrams);
        Assert.NotNull(reloaded.Product);
        Assert.Equal(snapshot.ProductId, reloaded.Product!.ProductId);
        Assert.Equal("Oats", reloaded.Product.Name);
        Assert.Equal(ProductCategory.GrainsAndBread, reloaded.Product.Category);
        Assert.Equal(380m, reloaded.Product.Per100Grams.Calories);
        Assert.Equal(60m, reloaded.Product.Per100Grams.Carbohydrates);
    }

    [Fact]
    public async Task ProductEntry_HasNoForeignKeyToTheCatalogProduct()
    {
        // The snapshot must survive the catalog product being deleted, exactly as recipe
        // ingredients do. A product entry is inserted for a product id that does not exist.
        using SqliteTestDatabase database = new();
        DateOnly date = new(2026, 5, 28);
        var entryId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            await plans.AddAsync(
                MealPlanEntry.ForProduct(entryId, date, OatsSnapshot(), MealType.Snack, 30m));
        }

        MealPlanEntry? reloaded;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            reloaded = await plans.GetByIdAsync(entryId);
        }

        Assert.NotNull(reloaded);
        Assert.Equal("Oats", reloaded!.Product!.Name);
    }

    [Fact]
    public async Task RecipeEntry_PersistsHalfPortions()
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
            await plans.AddAsync(
                MealPlanEntry.ForRecipe(entryId, date, recipeId, MealType.Breakfast, 1.5m));
        }

        MealPlanEntry? reloaded;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            reloaded = await plans.GetByIdAsync(entryId);
        }

        Assert.NotNull(reloaded);
        Assert.Equal(MealPlanEntrySource.Recipe, reloaded!.Source);
        Assert.Equal(1.5m, reloaded.RecipePortions);
        Assert.Null(reloaded.Product);
    }

    [Fact]
    public async Task ListByDateRangeAsync_ReturnsInclusiveWindowInStableOrder()
    {
        using SqliteTestDatabase database = new();
        DateOnly start = new(2026, 5, 25);
        var recipeId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            await recipes.AddAsync(new Recipe(recipeId, "Porridge", 2));

            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            // Inserted out of order: the read must not depend on insertion order.
            await plans.AddAsync(
                MealPlanEntry.ForRecipe(Guid.NewGuid(), start.AddDays(2), recipeId, MealType.Dinner, 1m));
            await plans.AddAsync(
                MealPlanEntry.ForRecipe(Guid.NewGuid(), start, recipeId, MealType.Breakfast, 1m));
            // Both boundaries are inclusive; the day after the window is not.
            await plans.AddAsync(
                MealPlanEntry.ForRecipe(Guid.NewGuid(), start.AddDays(3), recipeId, MealType.Lunch, 1m));
            await plans.AddAsync(
                MealPlanEntry.ForRecipe(Guid.NewGuid(), start.AddDays(-1), recipeId, MealType.Lunch, 1m));
        }

        IReadOnlyList<MealPlanEntry> entries;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            entries = await plans.ListByDateRangeAsync(start, start.AddDays(2));
        }

        Assert.Equal(2, entries.Count);
        Assert.Equal(start, entries[0].Date);
        Assert.Equal(start.AddDays(2), entries[1].Date);
    }

    [Fact]
    public async Task ListByDateRangeAsync_ReturnsOnlyCurrentUsersEntries()
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
                MealPlanEntry.ForRecipe(Guid.NewGuid(), date, ownerRecipeId, MealType.Breakfast, 1m));
            MealPlanRepository otherPlans = new(context, new TestCurrentUser(OtherId));
            await otherPlans.AddAsync(
                MealPlanEntry.ForRecipe(Guid.NewGuid(), date, otherRecipeId, MealType.Lunch, 1m));
        }

        IReadOnlyList<MealPlanEntry> entries;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            entries = await plans.ListByDateRangeAsync(date.AddDays(-7), date.AddDays(7));
        }

        Assert.Equal(ownerRecipeId, Assert.Single(entries).RecipeId);
    }

    [Fact]
    public async Task ListUsedRecipeIdsAsync_IgnoresProductEntries()
    {
        using SqliteTestDatabase database = new();
        DateOnly date = new(2026, 5, 28);
        var plannedId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            await recipes.AddAsync(new Recipe(plannedId, "Planned", 1));

            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            await plans.AddAsync(
                MealPlanEntry.ForRecipe(Guid.NewGuid(), date, plannedId, MealType.Breakfast, 1m));
            // A product entry has a null recipe_id and must not disturb the usage projection.
            await plans.AddAsync(
                MealPlanEntry.ForProduct(Guid.NewGuid(), date, OatsSnapshot(), MealType.Snack, 30m));
        }

        IReadOnlyCollection<Guid> used;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository repository = new(context, new TestCurrentUser(OwnerId));
            used = await repository.ListUsedRecipeIdsAsync([plannedId]);
        }

        Assert.Equal([plannedId], used);
    }

    [Fact]
    public async Task AddRangeAsync_PersistsEveryEntryForTheCurrentUser()
    {
        using SqliteTestDatabase database = new();
        DateOnly start = new(2026, 5, 25);
        var recipeId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            await recipes.AddAsync(new Recipe(recipeId, "Porridge", 2));

            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            await plans.AddRangeAsync(
            [
                MealPlanEntry.ForRecipe(Guid.NewGuid(), start, recipeId, MealType.Breakfast, 1.5m),
                MealPlanEntry.ForRecipe(Guid.NewGuid(), start.AddDays(1), recipeId, MealType.Breakfast, 1.5m),
                MealPlanEntry.ForProduct(Guid.NewGuid(), start.AddDays(2), OatsSnapshot(), MealType.Snack, 30m)
            ]);
        }

        IReadOnlyList<MealPlanEntry> entries;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            entries = await plans.ListByDateRangeAsync(start, start.AddDays(2));
        }

        Assert.Equal(3, entries.Count);
        Assert.Equal(1.5m, entries[0].RecipePortions);
        Assert.Equal(30m, entries[2].ProductGrams);
    }

    [Fact]
    public async Task AddRangeAsync_IsANoOp_ForAnEmptyBatch()
    {
        using SqliteTestDatabase database = new();
        DateOnly date = new(2026, 5, 28);

        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            await plans.AddRangeAsync([]);
        }

        await using (JadlifyDbContext verification = database.CreateContext())
        {
            MealPlanRepository plans = new(verification, new TestCurrentUser(OwnerId));
            Assert.Empty(await plans.ListByDateAsync(date));
        }
    }

    [Fact]
    public async Task AddRangeAsync_WritesNothing_WhenOneEntryInTheBatchIsRejected()
    {
        using SqliteTestDatabase database = new();
        DateOnly start = new(2026, 5, 25);
        var recipeId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            await recipes.AddAsync(new Recipe(recipeId, "Porridge", 2));
        }

        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));

            // The last entry references a recipe that does not exist, so its insert violates the
            // foreign key. The two valid entries in front of it must not survive on their own.
            await Assert.ThrowsAsync<DbUpdateException>(() => plans.AddRangeAsync(
            [
                MealPlanEntry.ForRecipe(Guid.NewGuid(), start, recipeId, MealType.Breakfast, 1m),
                MealPlanEntry.ForRecipe(Guid.NewGuid(), start.AddDays(1), recipeId, MealType.Lunch, 1m),
                MealPlanEntry.ForRecipe(Guid.NewGuid(), start.AddDays(2), Guid.NewGuid(), MealType.Dinner, 1m)
            ]));
        }

        await using (JadlifyDbContext verification = database.CreateContext())
        {
            MealPlanRepository plans = new(verification, new TestCurrentUser(OwnerId));
            Assert.Empty(await plans.ListByDateRangeAsync(start, start.AddDays(2)));
        }
    }

    [Fact]
    public async Task ReplaceDaysAsync_ClearsTargetDaysAndLeavesOnlyTheReplacements()
    {
        using SqliteTestDatabase database = new();
        DateOnly target = new(2026, 5, 28);
        DateOnly untouched = target.AddDays(1);
        var recipeId = Guid.NewGuid();
        var survivorId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            await recipes.AddAsync(new Recipe(recipeId, "Porridge", 2));

            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            await plans.AddAsync(
                MealPlanEntry.ForRecipe(Guid.NewGuid(), target, recipeId, MealType.Breakfast, 1m));
            // A day outside the replaced set must be left alone.
            await plans.AddAsync(
                MealPlanEntry.ForRecipe(survivorId, untouched, recipeId, MealType.Lunch, 1m));
        }

        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            await plans.ReplaceDaysAsync(
                [target],
                [MealPlanEntry.ForRecipe(Guid.NewGuid(), target, recipeId, MealType.Dinner, 2.5m)]);
        }

        await using (JadlifyDbContext verification = database.CreateContext())
        {
            MealPlanRepository plans = new(verification, new TestCurrentUser(OwnerId));

            MealPlanEntry replaced = Assert.Single(await plans.ListByDateAsync(target));
            Assert.Equal(MealType.Dinner, replaced.MealType);
            Assert.Equal(2.5m, replaced.RecipePortions);

            Assert.Equal(survivorId, Assert.Single(await plans.ListByDateAsync(untouched)).Id);
        }
    }

    [Fact]
    public async Task ReplaceDaysAsync_DoesNotRemoveAnotherUsersEntriesOnTheSameDay()
    {
        using SqliteTestDatabase database = new();
        DateOnly target = new(2026, 5, 28);
        var ownerRecipeId = Guid.NewGuid();
        var otherRecipeId = Guid.NewGuid();
        var otherEntryId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository ownerRecipes = new(context, new TestCurrentUser(OwnerId));
            await ownerRecipes.AddAsync(new Recipe(ownerRecipeId, "Porridge", 2));
            RecipeRepository otherRecipes = new(context, new TestCurrentUser(OtherId));
            await otherRecipes.AddAsync(new Recipe(otherRecipeId, "Salad", 1));

            MealPlanRepository ownerPlans = new(context, new TestCurrentUser(OwnerId));
            await ownerPlans.AddAsync(
                MealPlanEntry.ForRecipe(Guid.NewGuid(), target, ownerRecipeId, MealType.Breakfast, 1m));
            MealPlanRepository otherPlans = new(context, new TestCurrentUser(OtherId));
            await otherPlans.AddAsync(
                MealPlanEntry.ForRecipe(otherEntryId, target, otherRecipeId, MealType.Lunch, 1m));
        }

        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            await plans.ReplaceDaysAsync(
                [target],
                [MealPlanEntry.ForRecipe(Guid.NewGuid(), target, ownerRecipeId, MealType.Dinner, 1m)]);
        }

        await using (JadlifyDbContext verification = database.CreateContext())
        {
            MealPlanRepository otherPlans = new(verification, new TestCurrentUser(OtherId));
            Assert.Equal(otherEntryId, Assert.Single(await otherPlans.ListByDateAsync(target)).Id);
        }
    }

    [Fact]
    public async Task ReplaceDaysAsync_LeavesTheDayIntact_WhenAReplacementIsRejected()
    {
        using SqliteTestDatabase database = new();
        DateOnly target = new(2026, 5, 28);
        var recipeId = Guid.NewGuid();
        var originalId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            await recipes.AddAsync(new Recipe(recipeId, "Porridge", 2));

            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));
            await plans.AddAsync(
                MealPlanEntry.ForRecipe(originalId, target, recipeId, MealType.Breakfast, 1m));
        }

        await using (JadlifyDbContext context = database.CreateContext())
        {
            MealPlanRepository plans = new(context, new TestCurrentUser(OwnerId));

            // The clear and the insert are one transaction: a rejected replacement must not
            // leave the day emptied by the delete that came before it.
            await Assert.ThrowsAsync<DbUpdateException>(() => plans.ReplaceDaysAsync(
                [target],
                [
                    MealPlanEntry.ForRecipe(Guid.NewGuid(), target, recipeId, MealType.Lunch, 1m),
                    MealPlanEntry.ForRecipe(Guid.NewGuid(), target, Guid.NewGuid(), MealType.Dinner, 1m)
                ]));
        }

        await using (JadlifyDbContext verification = database.CreateContext())
        {
            MealPlanRepository plans = new(verification, new TestCurrentUser(OwnerId));
            MealPlanEntry surviving = Assert.Single(await plans.ListByDateAsync(target));
            Assert.Equal(originalId, surviving.Id);
            Assert.Equal(MealType.Breakfast, surviving.MealType);
        }
    }
}
