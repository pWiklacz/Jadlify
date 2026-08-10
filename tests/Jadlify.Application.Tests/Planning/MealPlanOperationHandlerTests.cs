using Jadlify.Application.Planning;
using Jadlify.Application.Planning.MealPlans.CopyMealPlanDay;
using Jadlify.Application.Planning.MealPlans.CopyMealPlanEntry;
using Jadlify.Application.Planning.MealPlans.MoveMealPlanEntry;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Products;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Planning;

/// <summary>
/// Covers the atomic planner operations: move, copy-entry, and copy-day. The repository fake
/// applies a batch all-or-nothing exactly as the real one does, so these tests pin the
/// handlers' validate-everything-before-writing order rather than the persistence mechanics.
/// </summary>
public class MealPlanOperationHandlerTests
{
    private static readonly DateOnly Day = new(2026, 6, 2);

    private static PlannedProductSnapshot Snapshot(string name = "Oats") =>
        new(Guid.NewGuid(), name, new MacroNutrients(380m, 13m, 7m, 60m), ProductCategory.GrainsAndBread);

    private static MealPlanEntry RecipeEntry(
        DateOnly? date = null,
        MealType mealType = MealType.Breakfast,
        decimal portions = 1m) =>
        MealPlanEntry.ForRecipe(Guid.NewGuid(), date ?? Day, Guid.NewGuid(), mealType, portions);

    private static MealPlanEntry ProductEntry(
        DateOnly? date = null,
        MealType mealType = MealType.Snack,
        decimal grams = 45m) =>
        MealPlanEntry.ForProduct(Guid.NewGuid(), date ?? Day, Snapshot(), mealType, grams);

    public class Move
    {
        [Fact]
        public async Task ReschedulesEntry_KeepingIdentitySourceAndQuantity()
        {
            MealPlanEntry entry = RecipeEntry(portions: 2.5m);
            var mealPlans = new FakeMealPlanRepository(entry);
            MoveMealPlanEntryCommandHandler handler = new(mealPlans);
            DateOnly target = Day.AddDays(4);

            Result result = await handler.HandleAsync(
                new MoveMealPlanEntryCommand(entry.Id, target, MealType.Dinner),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            MealPlanEntry moved = Assert.Single(mealPlans.Entries);
            Assert.Equal(entry.Id, moved.Id);
            Assert.Equal(target, moved.Date);
            Assert.Equal(MealType.Dinner, moved.MealType);
            Assert.Equal(2.5m, moved.RecipePortions);
            Assert.Equal(1, mealPlans.UpdateCount);
        }

        [Fact]
        public async Task KeepsProductSnapshot()
        {
            MealPlanEntry entry = ProductEntry(grams: 80m);
            Guid productId = entry.Product!.ProductId;
            var mealPlans = new FakeMealPlanRepository(entry);
            MoveMealPlanEntryCommandHandler handler = new(mealPlans);

            Result result = await handler.HandleAsync(
                new MoveMealPlanEntryCommand(entry.Id, Day.AddDays(1), MealType.Lunch),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            MealPlanEntry moved = Assert.Single(mealPlans.Entries);
            Assert.Equal(productId, moved.Product!.ProductId);
            Assert.Equal(80m, moved.ProductGrams);
        }

        [Fact]
        public async Task ReportsNotFound_ForMissingOrCrossUserEntry()
        {
            var mealPlans = new FakeMealPlanRepository();
            MoveMealPlanEntryCommandHandler handler = new(mealPlans);

            Result result = await handler.HandleAsync(
                new MoveMealPlanEntryCommand(Guid.NewGuid(), Day, MealType.Lunch),
                CancellationToken.None);

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.NotFound, result.Error.Type);
            Assert.Equal(0, mealPlans.UpdateCount);
        }
    }

    public class CopyEntry
    {
        [Fact]
        public async Task CreatesOneCopyPerTargetDay_LeavingTheOriginalInPlace()
        {
            MealPlanEntry entry = RecipeEntry(mealType: MealType.Dinner, portions: 1.5m);
            var mealPlans = new FakeMealPlanRepository(entry);
            CopyMealPlanEntryCommandHandler handler = new(mealPlans);
            DateOnly[] targets = [Day.AddDays(1), Day.AddDays(2), Day.AddDays(3)];

            Result<IReadOnlyList<CreatedMealPlanEntryDto>> result = await handler.HandleAsync(
                new CopyMealPlanEntryCommand(entry.Id, targets),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.Value.Count);
            Assert.Equal(targets.ToList(), result.Value.Select(created => created.Date).ToList());
            Assert.All(result.Value, created => Assert.Equal(MealType.Dinner, created.MealType));

            // Original plus three copies, all with distinct ids.
            Assert.Equal(4, mealPlans.Entries.Count);
            Assert.Contains(mealPlans.Entries, stored => stored.Id == entry.Id && stored.Date == Day);
            Assert.Equal(4, mealPlans.Entries.Select(stored => stored.Id).Distinct().Count());
            Assert.All(mealPlans.Entries, stored => Assert.Equal(1.5m, stored.RecipePortions));

            // One batched write, not one per target day.
            Assert.Equal(1, mealPlans.AddRangeCount);
        }

        [Fact]
        public async Task CopiesProductEntryWithItsOwnSnapshot()
        {
            MealPlanEntry entry = ProductEntry(grams: 60m);
            var mealPlans = new FakeMealPlanRepository(entry);
            CopyMealPlanEntryCommandHandler handler = new(mealPlans);

            Result<IReadOnlyList<CreatedMealPlanEntryDto>> result = await handler.HandleAsync(
                new CopyMealPlanEntryCommand(entry.Id, [Day.AddDays(1)]),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            MealPlanEntry copy = Assert.Single(mealPlans.Entries, stored => stored.Id != entry.Id);
            Assert.Equal(MealPlanEntrySource.Product, copy.Source);
            Assert.NotSame(entry.Product, copy.Product);
            Assert.Equal(entry.Product!.ProductId, copy.Product!.ProductId);
            Assert.Equal(60m, copy.ProductGrams);
        }

        [Fact]
        public async Task ReturnsCopiesInTargetDateOrder_RegardlessOfRequestOrder()
        {
            MealPlanEntry entry = RecipeEntry();
            var mealPlans = new FakeMealPlanRepository(entry);
            CopyMealPlanEntryCommandHandler handler = new(mealPlans);

            Result<IReadOnlyList<CreatedMealPlanEntryDto>> result = await handler.HandleAsync(
                new CopyMealPlanEntryCommand(entry.Id, [Day.AddDays(5), Day.AddDays(1), Day.AddDays(3)]),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(
                [Day.AddDays(1), Day.AddDays(3), Day.AddDays(5)],
                [.. result.Value.Select(created => created.Date)]);
        }

        [Fact]
        public async Task WritesNothing_WhenSourceIsMissingOrCrossUser()
        {
            var mealPlans = new FakeMealPlanRepository();
            CopyMealPlanEntryCommandHandler handler = new(mealPlans);

            Result<IReadOnlyList<CreatedMealPlanEntryDto>> result = await handler.HandleAsync(
                new CopyMealPlanEntryCommand(Guid.NewGuid(), [Day.AddDays(1)]),
                CancellationToken.None);

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.NotFound, result.Error.Type);
            Assert.Empty(mealPlans.Entries);
            Assert.Equal(0, mealPlans.AddRangeCount);
        }
    }

    public class CopyDay
    {
        private static FakeMealPlanRepository SourceDayWithTwoEntries() =>
            new(
                RecipeEntry(mealType: MealType.Breakfast, portions: 1m),
                ProductEntry(mealType: MealType.Snack, grams: 30m));

        [Fact]
        public async Task Add_AppendsCopiesAndKeepsTargetDaysExistingEntries()
        {
            MealPlanEntry existingOnTarget = RecipeEntry(date: Day.AddDays(1), mealType: MealType.Dinner);
            var mealPlans = new FakeMealPlanRepository(
                RecipeEntry(mealType: MealType.Breakfast),
                ProductEntry(mealType: MealType.Snack),
                existingOnTarget);
            CopyMealPlanDayCommandHandler handler = new(mealPlans);

            Result<IReadOnlyList<CreatedMealPlanEntryDto>> result = await handler.HandleAsync(
                new CopyMealPlanDayCommand(Day, [Day.AddDays(1)], MealPlanDayCopyMode.Add),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);
            Assert.Contains(mealPlans.Entries, stored => stored.Id == existingOnTarget.Id);
            Assert.Equal(3, mealPlans.Entries.Count(stored => stored.Date == Day.AddDays(1)));
            Assert.Equal(1, mealPlans.AddRangeCount);
            Assert.Equal(0, mealPlans.ReplaceDaysCount);
        }

        [Fact]
        public async Task Replace_ClearsTargetDayAndLeavesOnlyTheCopies()
        {
            MealPlanEntry existingOnTarget = RecipeEntry(date: Day.AddDays(1), mealType: MealType.Dinner);
            var mealPlans = new FakeMealPlanRepository(
                RecipeEntry(mealType: MealType.Breakfast),
                ProductEntry(mealType: MealType.Snack),
                existingOnTarget);
            CopyMealPlanDayCommandHandler handler = new(mealPlans);

            Result<IReadOnlyList<CreatedMealPlanEntryDto>> result = await handler.HandleAsync(
                new CopyMealPlanDayCommand(Day, [Day.AddDays(1)], MealPlanDayCopyMode.Replace),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.DoesNotContain(mealPlans.Entries, stored => stored.Id == existingOnTarget.Id);
            Assert.Equal(2, mealPlans.Entries.Count(stored => stored.Date == Day.AddDays(1)));
            // The clear and the insert go through one call, so the day is never left empty.
            Assert.Equal(1, mealPlans.ReplaceDaysCount);
            Assert.Equal([Day.AddDays(1)], mealPlans.ClearedDates);
            Assert.Equal(0, mealPlans.AddRangeCount);
        }

        [Fact]
        public async Task CopiesEveryEntryOntoEveryTargetDay()
        {
            FakeMealPlanRepository mealPlans = SourceDayWithTwoEntries();
            CopyMealPlanDayCommandHandler handler = new(mealPlans);
            DateOnly[] targets = [Day.AddDays(1), Day.AddDays(2), Day.AddDays(3)];

            Result<IReadOnlyList<CreatedMealPlanEntryDto>> result = await handler.HandleAsync(
                new CopyMealPlanDayCommand(Day, targets, MealPlanDayCopyMode.Add),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(6, result.Value.Count);
            Assert.All(targets, target =>
                Assert.Equal(2, mealPlans.Entries.Count(stored => stored.Date == target)));
        }

        [Fact]
        public async Task RejectsEmptySourceDay_WithoutClearingAnyTarget()
        {
            MealPlanEntry existingOnTarget = RecipeEntry(date: Day.AddDays(1));
            var mealPlans = new FakeMealPlanRepository(existingOnTarget);
            CopyMealPlanDayCommandHandler handler = new(mealPlans);

            // Replace with an empty source would otherwise be a silent way to wipe days.
            Result<IReadOnlyList<CreatedMealPlanEntryDto>> result = await handler.HandleAsync(
                new CopyMealPlanDayCommand(Day, [Day.AddDays(1)], MealPlanDayCopyMode.Replace),
                CancellationToken.None);

            Assert.True(result.IsFailure);
            Assert.IsType<ValidationError>(result.Error);
            Assert.Single(mealPlans.Entries);
            Assert.Equal(0, mealPlans.ReplaceDaysCount);
            Assert.Empty(mealPlans.ClearedDates);
        }

        [Fact]
        public async Task RejectsBatchesOverTheCreatedEntryCeiling_BeforeWriting()
        {
            // A dense source day multiplied across many targets: the target count alone is
            // within bounds, so the ceiling has to be checked on the product of the two.
            int perDay = PlanningValidationBounds.MaxCopyCreatedEntries
                / PlanningValidationBounds.MaxCopyTargetDays + 2;
            MealPlanEntry[] source = [.. Enumerable.Range(0, perDay).Select(_ => RecipeEntry())];
            var mealPlans = new FakeMealPlanRepository(source);
            CopyMealPlanDayCommandHandler handler = new(mealPlans);
            DateOnly[] targets =
            [
                .. Enumerable
                    .Range(1, PlanningValidationBounds.MaxCopyTargetDays)
                    .Select(offset => Day.AddDays(offset))
            ];

            Result<IReadOnlyList<CreatedMealPlanEntryDto>> result = await handler.HandleAsync(
                new CopyMealPlanDayCommand(Day, targets, MealPlanDayCopyMode.Add),
                CancellationToken.None);

            Assert.True(result.IsFailure);
            Assert.IsType<ValidationError>(result.Error);
            Assert.Equal(perDay, mealPlans.Entries.Count);
            Assert.Equal(0, mealPlans.AddRangeCount);
        }
    }
}
