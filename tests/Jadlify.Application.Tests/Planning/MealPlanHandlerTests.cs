using Jadlify.Application.Planning;
using Jadlify.Application.Planning.MealPlans.AddMealPlanEntry;
using Jadlify.Application.Planning.MealPlans.DeleteMealPlanEntry;
using Jadlify.Application.Planning.MealPlans.GetMealPlanRange;
using Jadlify.Application.Planning.MealPlans.ListMealPlanEntries;
using Jadlify.Application.Planning.MealPlans.UpdateMealPlanEntry;
using Jadlify.Application.Tests.Products;
using Jadlify.Application.Tests.Recipes;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Products;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Planning;

public class MealPlanHandlerTests
{
    private static readonly DateOnly Day = new(2026, 6, 2);

    private static Product Oats() =>
        new(
            Guid.NewGuid(),
            "Oats",
            new MacroNutrients(380m, 13m, 7m, 60m),
            category: ProductCategory.GrainsAndBread);

    private static AddMealPlanEntryCommandHandler AddHandler(
        FakeMealPlanRepository mealPlans,
        FakeRecipeRepository? recipes = null,
        FakeProductRepository? products = null) =>
        new(mealPlans, recipes ?? new FakeRecipeRepository(), products ?? new FakeProductRepository());

    [Fact]
    public async Task AddEntry_AddsRecipeEntry_WhenRecipeExistsForUser()
    {
        var recipe = new Recipe(Guid.NewGuid(), "Porridge", 2);
        var mealPlans = new FakeMealPlanRepository();
        AddMealPlanEntryCommandHandler handler = AddHandler(mealPlans, new FakeRecipeRepository(recipe));

        Result<Guid> result = await handler.HandleAsync(
            new AddMealPlanEntryCommand(Day, MealType.Breakfast, RecipeId: recipe.Id, Portions: 2m),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        MealPlanEntry entry = Assert.Single(mealPlans.Entries);
        Assert.Equal(result.Value, entry.Id);
        Assert.Equal(Day, entry.Date);
        Assert.Equal(MealPlanEntrySource.Recipe, entry.Source);
        Assert.Equal(recipe.Id, entry.RecipeId);
        Assert.Equal(MealType.Breakfast, entry.MealType);
        Assert.Equal(2m, entry.RecipePortions);
        Assert.Null(entry.Product);
    }

    [Fact]
    public async Task AddEntry_AddsHalfPortionRecipeEntry()
    {
        var recipe = new Recipe(Guid.NewGuid(), "Porridge", 2);
        var mealPlans = new FakeMealPlanRepository();
        AddMealPlanEntryCommandHandler handler = AddHandler(mealPlans, new FakeRecipeRepository(recipe));

        Result<Guid> result = await handler.HandleAsync(
            new AddMealPlanEntryCommand(Day, MealType.Snack, RecipeId: recipe.Id, Portions: 0.5m),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0.5m, Assert.Single(mealPlans.Entries).RecipePortions);
    }

    [Fact]
    public async Task AddEntry_AddsProductEntry_WithSnapshotOfCurrentCatalogState()
    {
        Product oats = Oats();
        var mealPlans = new FakeMealPlanRepository();
        AddMealPlanEntryCommandHandler handler =
            AddHandler(mealPlans, products: new FakeProductRepository(oats));

        Result<Guid> result = await handler.HandleAsync(
            new AddMealPlanEntryCommand(Day, MealType.Snack, ProductId: oats.Id, Grams: 45m),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        MealPlanEntry entry = Assert.Single(mealPlans.Entries);
        Assert.Equal(MealPlanEntrySource.Product, entry.Source);
        Assert.Null(entry.RecipeId);
        Assert.Equal(45m, entry.ProductGrams);
        Assert.NotNull(entry.Product);
        Assert.Equal(oats.Id, entry.Product!.ProductId);
        Assert.Equal("Oats", entry.Product.Name);
        Assert.Equal(ProductCategory.GrainsAndBread, entry.Product.Category);
        Assert.Equal(380m, entry.Product.Per100Grams.Calories);
    }

    [Fact]
    public async Task AddEntry_ReturnsValidationFailure_WhenRecipeMissingOrCrossUser()
    {
        // The owner-scoped recipe repository returns null for both a missing recipe and
        // another user's recipe, so this single path covers cross-user rejection too.
        var mealPlans = new FakeMealPlanRepository();
        AddMealPlanEntryCommandHandler handler = AddHandler(mealPlans);

        Result<Guid> result = await handler.HandleAsync(
            new AddMealPlanEntryCommand(Day, MealType.Lunch, RecipeId: Guid.NewGuid(), Portions: 1m),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Empty(mealPlans.Entries);
    }

    [Fact]
    public async Task AddEntry_ReturnsValidationFailure_WhenProductMissingOrCrossUser()
    {
        var mealPlans = new FakeMealPlanRepository();
        AddMealPlanEntryCommandHandler handler = AddHandler(mealPlans);

        Result<Guid> result = await handler.HandleAsync(
            new AddMealPlanEntryCommand(Day, MealType.Snack, ProductId: Guid.NewGuid(), Grams: 30m),
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
        AddMealPlanEntryCommandHandler handler = AddHandler(mealPlans, new FakeRecipeRepository(recipe));
        var command = new AddMealPlanEntryCommand(Day, MealType.Breakfast, RecipeId: recipe.Id, Portions: 1m);

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
        var entry = MealPlanEntry.ForRecipe(Guid.NewGuid(), Day, recipe.Id, MealType.Breakfast, 1m);
        var otherDayEntry =
            MealPlanEntry.ForRecipe(Guid.NewGuid(), Day.AddDays(1), recipe.Id, MealType.Dinner, 1m);
        var mealPlans = new FakeMealPlanRepository(entry, otherDayEntry);
        var handler = new ListMealPlanEntriesQueryHandler(mealPlans, new FakeRecipeRepository(recipe));

        Result<IReadOnlyList<MealPlanEntryDto>> result =
            await handler.HandleAsync(new ListMealPlanEntriesQuery(Day), CancellationToken.None);

        Assert.True(result.IsSuccess);
        MealPlanEntryDto dto = Assert.Single(result.Value);
        Assert.Equal(entry.Id, dto.Id);
        Assert.Equal(MealPlanEntrySource.Recipe, dto.Source);
        Assert.Equal(recipe.Id, dto.RecipeId);
        Assert.Equal("Porridge", dto.RecipeName);
        Assert.Equal(1m, dto.Portions);
        Assert.Equal(MealType.Breakfast, dto.MealType);
        Assert.Null(dto.ProductId);
        Assert.Null(dto.Grams);
    }

    [Fact]
    public async Task ListEntries_ProjectsProductEntryFromItsOwnSnapshot()
    {
        Product oats = Oats();
        var entry = MealPlanEntry.ForProduct(
            Guid.NewGuid(),
            Day,
            PlannedProductSnapshot.FromProduct(oats),
            MealType.Snack,
            45m);
        var mealPlans = new FakeMealPlanRepository(entry);
        var handler = new ListMealPlanEntriesQueryHandler(mealPlans, new FakeRecipeRepository());

        Result<IReadOnlyList<MealPlanEntryDto>> result =
            await handler.HandleAsync(new ListMealPlanEntriesQuery(Day), CancellationToken.None);

        Assert.True(result.IsSuccess);
        MealPlanEntryDto dto = Assert.Single(result.Value);
        Assert.Equal(MealPlanEntrySource.Product, dto.Source);
        Assert.Equal(oats.Id, dto.ProductId);
        Assert.Equal("Oats", dto.ProductName);
        Assert.Equal(ProductCategory.GrainsAndBread, dto.Category);
        Assert.Equal(45m, dto.Grams);
        Assert.Null(dto.RecipeId);
        Assert.Null(dto.Portions);
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
        var entry = MealPlanEntry.ForRecipe(Guid.NewGuid(), Day, recipeId, MealType.Breakfast, 1m);
        var mealPlans = new FakeMealPlanRepository(entry);
        var handler = new UpdateMealPlanEntryCommandHandler(mealPlans);

        Result result = await handler.HandleAsync(
            new UpdateMealPlanEntryCommand(entry.Id, MealType.Dinner, Portions: 2.5m),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, mealPlans.UpdateCount);
        MealPlanEntry updated = Assert.Single(mealPlans.Entries);
        Assert.Equal(Day, updated.Date);
        Assert.Equal(recipeId, updated.RecipeId);
        Assert.Equal(MealType.Dinner, updated.MealType);
        Assert.Equal(2.5m, updated.RecipePortions);
    }

    [Fact]
    public async Task UpdateEntry_ChangesGrams_ForProductEntry()
    {
        var entry = MealPlanEntry.ForProduct(
            Guid.NewGuid(),
            Day,
            PlannedProductSnapshot.FromProduct(Oats()),
            MealType.Snack,
            30m);
        var mealPlans = new FakeMealPlanRepository(entry);
        var handler = new UpdateMealPlanEntryCommandHandler(mealPlans);

        Result result = await handler.HandleAsync(
            new UpdateMealPlanEntryCommand(entry.Id, MealType.Lunch, Grams: 120m),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        MealPlanEntry updated = Assert.Single(mealPlans.Entries);
        Assert.Equal(MealType.Lunch, updated.MealType);
        Assert.Equal(120m, updated.ProductGrams);
    }

    [Fact]
    public async Task UpdateEntry_RejectsGrams_ForRecipeEntry()
    {
        // Grams against a recipe entry would silently rescale the meal, so it is a client error.
        var entry = MealPlanEntry.ForRecipe(Guid.NewGuid(), Day, Guid.NewGuid(), MealType.Breakfast, 1m);
        var mealPlans = new FakeMealPlanRepository(entry);
        var handler = new UpdateMealPlanEntryCommandHandler(mealPlans);

        Result result = await handler.HandleAsync(
            new UpdateMealPlanEntryCommand(entry.Id, MealType.Breakfast, Grams: 100m),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, mealPlans.UpdateCount);
        Assert.Equal(1m, Assert.Single(mealPlans.Entries).RecipePortions);
    }

    [Fact]
    public async Task UpdateEntry_RejectsPortions_ForProductEntry()
    {
        var entry = MealPlanEntry.ForProduct(
            Guid.NewGuid(),
            Day,
            PlannedProductSnapshot.FromProduct(Oats()),
            MealType.Snack,
            30m);
        var mealPlans = new FakeMealPlanRepository(entry);
        var handler = new UpdateMealPlanEntryCommandHandler(mealPlans);

        Result result = await handler.HandleAsync(
            new UpdateMealPlanEntryCommand(entry.Id, MealType.Snack, Portions: 2m),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, mealPlans.UpdateCount);
        Assert.Equal(30m, Assert.Single(mealPlans.Entries).ProductGrams);
    }

    [Fact]
    public async Task UpdateEntry_ReturnsNotFound_WhenEntryMissingOrCrossUser()
    {
        var mealPlans = new FakeMealPlanRepository();
        var handler = new UpdateMealPlanEntryCommandHandler(mealPlans);

        Result result = await handler.HandleAsync(
            new UpdateMealPlanEntryCommand(Guid.NewGuid(), MealType.Dinner, Portions: 2m),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal(0, mealPlans.UpdateCount);
    }

    [Fact]
    public async Task DeleteEntry_DelegatesToRepository()
    {
        var entry = MealPlanEntry.ForRecipe(Guid.NewGuid(), Day, Guid.NewGuid(), MealType.Breakfast, 1m);
        var mealPlans = new FakeMealPlanRepository(entry);
        var handler = new DeleteMealPlanEntryCommandHandler(mealPlans);

        Result result = await handler.HandleAsync(
            new DeleteMealPlanEntryCommand(entry.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(entry.Id, Assert.Single(mealPlans.DeletedIds));
        Assert.Empty(mealPlans.Entries);
    }

    public class Range
    {
        private static readonly DateOnly Start = new(2026, 6, 1);

        // 400 kcal across the whole recipe, 2 portions -> 200 kcal per serving.
        private static Recipe Porridge() =>
            new(
                Guid.NewGuid(),
                "Porridge",
                2,
                [
                    new RecipeIngredient(
                        Guid.NewGuid(),
                        "Oats",
                        new MacroNutrients(400m, 10m, 8m, 60m),
                        new GramAmount(100m))
                ]);

        private static GetMealPlanRangeQueryHandler Handler(
            FakeMealPlanRepository mealPlans,
            FakeRecipeRepository recipes,
            FakeDailyMacroGoalRepository? goals = null) =>
            new(mealPlans, recipes, goals ?? new FakeDailyMacroGoalRepository());

        [Fact]
        public async Task ReturnsEveryDayInRange_IncludingEmptyOnes()
        {
            // The month grid renders 42 cells and must not have to infer the gaps itself.
            GetMealPlanRangeQueryHandler handler =
                Handler(new FakeMealPlanRepository(), new FakeRecipeRepository());

            Result<MealPlanRangeDto> result = await handler.HandleAsync(
                new GetMealPlanRangeQuery(Start, Start.AddDays(41)),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value.Days.Count);
            Assert.Equal(Start, result.Value.Days[0].Date);
            Assert.Equal(Start.AddDays(41), result.Value.Days[^1].Date);
            Assert.All(result.Value.Days, day => Assert.Empty(day.Entries));
        }

        [Fact]
        public async Task GroupsEntriesByDayAndExcludesDatesOutsideRange()
        {
            Recipe recipe = Porridge();
            var inRange =
                MealPlanEntry.ForRecipe(Guid.NewGuid(), Start.AddDays(1), recipe.Id, MealType.Lunch, 1m);
            var outOfRange =
                MealPlanEntry.ForRecipe(Guid.NewGuid(), Start.AddDays(9), recipe.Id, MealType.Lunch, 1m);
            GetMealPlanRangeQueryHandler handler = Handler(
                new FakeMealPlanRepository(inRange, outOfRange),
                new FakeRecipeRepository(recipe));

            Result<MealPlanRangeDto> result = await handler.HandleAsync(
                new GetMealPlanRangeQuery(Start, Start.AddDays(6)),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(7, result.Value.Days.Count);
            Assert.Empty(result.Value.Days[0].Entries);
            Assert.Equal(inRange.Id, Assert.Single(result.Value.Days[1].Entries).Id);
            Assert.All(result.Value.Days.Skip(2), day => Assert.Empty(day.Entries));
        }

        [Fact]
        public async Task ReadsRecipesOnceForTheWholeRange()
        {
            // The whole point of the range read: one batched recipe load, not one per day.
            Recipe recipe = Porridge();
            MealPlanEntry[] entries =
            [
                .. Enumerable.Range(0, 7).Select(offset => MealPlanEntry.ForRecipe(
                    Guid.NewGuid(),
                    Start.AddDays(offset),
                    recipe.Id,
                    MealType.Lunch,
                    1m))
            ];
            var recipes = new FakeRecipeRepository(recipe);
            GetMealPlanRangeQueryHandler handler = Handler(new FakeMealPlanRepository(entries), recipes);

            Result<MealPlanRangeDto> result = await handler.HandleAsync(
                new GetMealPlanRangeQuery(Start, Start.AddDays(6)),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, recipes.ListByIdsWithIngredientsCallCount);
        }

        [Fact]
        public async Task SkipsRecipeRead_WhenRangeHasOnlyProductEntries()
        {
            var entry = MealPlanEntry.ForProduct(
                Guid.NewGuid(),
                Start,
                PlannedProductSnapshot.FromProduct(Oats()),
                MealType.Snack,
                50m);
            var recipes = new FakeRecipeRepository();
            GetMealPlanRangeQueryHandler handler = Handler(new FakeMealPlanRepository(entry), recipes);

            Result<MealPlanRangeDto> result = await handler.HandleAsync(
                new GetMealPlanRangeQuery(Start, Start.AddDays(6)),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(0, recipes.ListByIdsWithIngredientsCallCount);
            // Oracle: 380 kcal/100 g * 50 g = 190 kcal.
            Assert.Equal(190m, result.Value.Days[0].Total.Calories);
        }

        [Fact]
        public async Task TotalsMixedRecipeAndProductEntriesOnOneDay()
        {
            Recipe recipe = Porridge();
            var recipeEntry =
                MealPlanEntry.ForRecipe(Guid.NewGuid(), Start, recipe.Id, MealType.Breakfast, 0.5m);
            var productEntry = MealPlanEntry.ForProduct(
                Guid.NewGuid(),
                Start,
                PlannedProductSnapshot.FromProduct(Oats()),
                MealType.Snack,
                50m);
            GetMealPlanRangeQueryHandler handler = Handler(
                new FakeMealPlanRepository(recipeEntry, productEntry),
                new FakeRecipeRepository(recipe));

            Result<MealPlanRangeDto> result = await handler.HandleAsync(
                new GetMealPlanRangeQuery(Start, Start),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            MealPlanDayDto day = Assert.Single(result.Value.Days);
            // Oracle: 400 kcal / 2 portions = 200 per serving; 0.5 serving = 100 kcal.
            // Product: 380 kcal/100 g * 50 g = 190 kcal. Day total = 290 kcal.
            Assert.Equal(290m, day.Total.Calories);
            Assert.Equal(2, day.EntryMacros.Count);
            Assert.Equal(100m, Assert.Single(day.EntryMacros, m => m.EntryId == recipeEntry.Id).Macros.Calories);
            Assert.Equal(190m, Assert.Single(day.EntryMacros, m => m.EntryId == productEntry.Id).Macros.Calories);
        }

        [Fact]
        public async Task RepeatsGoalOnEveryDayWithSignedRemaining()
        {
            Recipe recipe = Porridge();
            var entry =
                MealPlanEntry.ForRecipe(Guid.NewGuid(), Start, recipe.Id, MealType.Breakfast, 1m);
            var goals = new FakeDailyMacroGoalRepository(
                new DailyMacroGoal(Guid.NewGuid(), new MacroNutrients(150m, 20m, 10m, 30m)));
            GetMealPlanRangeQueryHandler handler = Handler(
                new FakeMealPlanRepository(entry),
                new FakeRecipeRepository(recipe),
                goals);

            Result<MealPlanRangeDto> result = await handler.HandleAsync(
                new GetMealPlanRangeQuery(Start, Start.AddDays(1)),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            // Oracle: 200 kcal planned against a 150 kcal goal leaves -50 remaining.
            Assert.Equal(-50m, result.Value.Days[0].Remaining!.Calories);
            // An empty day still carries the goal, with the whole target still remaining.
            Assert.Equal(150m, result.Value.Days[1].Remaining!.Calories);
            Assert.NotNull(result.Value.Days[1].Goal);
        }

        [Fact]
        public async Task ReturnsNullGoalAndRemaining_WhenNoGoalConfigured()
        {
            GetMealPlanRangeQueryHandler handler = Handler(
                new FakeMealPlanRepository(),
                new FakeRecipeRepository());

            Result<MealPlanRangeDto> result = await handler.HandleAsync(
                new GetMealPlanRangeQuery(Start, Start),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            MealPlanDayDto day = Assert.Single(result.Value.Days);
            Assert.Null(day.Goal);
            Assert.Null(day.Remaining);
        }

        private static Product Oats() =>
            new(
                Guid.NewGuid(),
                "Oats",
                new MacroNutrients(380m, 13m, 7m, 60m),
                category: ProductCategory.GrainsAndBread);
    }
}
