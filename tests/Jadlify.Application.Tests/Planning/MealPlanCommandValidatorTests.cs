using Jadlify.Application.Planning.MealPlans.AddMealPlanEntry;
using Jadlify.Application.Planning.MealPlans.DeleteMealPlanEntry;
using Jadlify.Application.Planning.MealPlans.GetMealPlanRange;
using Jadlify.Application.Planning.MealPlans.UpdateMealPlanEntry;
using Jadlify.Domain.Planning;

namespace Jadlify.Application.Tests.Planning;

public class MealPlanCommandValidatorTests
{
    private static readonly DateOnly Day = new(2026, 6, 2);

    public class AddValidator
    {
        private readonly AddMealPlanEntryCommandValidator _validator = new();

        private static AddMealPlanEntryCommand ValidRecipe(
            Guid? recipeId = null,
            MealType mealType = MealType.Breakfast,
            decimal portions = 1m) =>
            new(Day, mealType, RecipeId: recipeId ?? Guid.NewGuid(), Portions: portions);

        private static AddMealPlanEntryCommand ValidProduct(
            Guid? productId = null,
            MealType mealType = MealType.Snack,
            decimal grams = 50m) =>
            new(Day, mealType, ProductId: productId ?? Guid.NewGuid(), Grams: grams);

        [Fact]
        public void Accepts_ValidRecipeEntry()
        {
            Assert.True(_validator.Validate(ValidRecipe()).IsValid);
        }

        [Fact]
        public void Accepts_ValidProductEntry()
        {
            Assert.True(_validator.Validate(ValidProduct()).IsValid);
        }

        [Theory]
        [InlineData(0.5)]
        [InlineData(1.5)]
        [InlineData(100)]
        public void Accepts_HalfPortionSteps(decimal portions)
        {
            Assert.True(_validator.Validate(ValidRecipe(portions: portions)).IsValid);
        }

        [Theory]
        [InlineData(0.25)]
        [InlineData(1.1)]
        [InlineData(0.75)]
        public void Rejects_OffStepPortions(decimal portions)
        {
            Assert.False(_validator.Validate(ValidRecipe(portions: portions)).IsValid);
        }

        [Fact]
        public void Rejects_EmptyRecipeId()
        {
            Assert.False(_validator.Validate(ValidRecipe(recipeId: Guid.Empty)).IsValid);
        }

        [Fact]
        public void Rejects_EmptyProductId()
        {
            Assert.False(_validator.Validate(ValidProduct(productId: Guid.Empty)).IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1000)]
        public void Rejects_NonPositiveOrUnrealisticPortions(decimal portions)
        {
            Assert.False(_validator.Validate(ValidRecipe(portions: portions)).IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        [InlineData(50_000)]
        public void Rejects_NonPositiveOrUnrealisticGrams(decimal grams)
        {
            Assert.False(_validator.Validate(ValidProduct(grams: grams)).IsValid);
        }

        [Fact]
        public void Rejects_UndefinedMealType()
        {
            Assert.False(_validator.Validate(ValidRecipe(mealType: (MealType)99)).IsValid);
        }

        [Fact]
        public void Rejects_NoSource()
        {
            // Neither variant supplied: there is nothing to plan.
            Assert.False(_validator.Validate(new AddMealPlanEntryCommand(Day, MealType.Lunch)).IsValid);
        }

        [Fact]
        public void Rejects_BothSources()
        {
            // Ambiguous: an entry has exactly one source, and picking one here would be a guess.
            var command = new AddMealPlanEntryCommand(
                Day,
                MealType.Lunch,
                RecipeId: Guid.NewGuid(),
                Portions: 1m,
                ProductId: Guid.NewGuid(),
                Grams: 50m);

            Assert.False(_validator.Validate(command).IsValid);
        }

        [Fact]
        public void Rejects_MixedVariantFields()
        {
            // A recipe id paired with grams is a half-filled variant, not a product entry.
            var command = new AddMealPlanEntryCommand(
                Day,
                MealType.Lunch,
                RecipeId: Guid.NewGuid(),
                Grams: 50m);

            Assert.False(_validator.Validate(command).IsValid);
        }
    }

    public class UpdateValidator
    {
        private readonly UpdateMealPlanEntryCommandValidator _validator = new();

        private static UpdateMealPlanEntryCommand ValidPortions(
            Guid? id = null,
            MealType mealType = MealType.Dinner,
            decimal portions = 2m) =>
            new(id ?? Guid.NewGuid(), mealType, Portions: portions);

        [Fact]
        public void Accepts_ValidPortionsUpdate()
        {
            Assert.True(_validator.Validate(ValidPortions()).IsValid);
        }

        [Fact]
        public void Accepts_ValidGramsUpdate()
        {
            var command = new UpdateMealPlanEntryCommand(Guid.NewGuid(), MealType.Snack, Grams: 120m);

            Assert.True(_validator.Validate(command).IsValid);
        }

        [Fact]
        public void Rejects_EmptyId()
        {
            Assert.False(_validator.Validate(ValidPortions(id: Guid.Empty)).IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0.25)]
        public void Rejects_NonPositiveOrOffStepPortions(decimal portions)
        {
            Assert.False(_validator.Validate(ValidPortions(portions: portions)).IsValid);
        }

        [Fact]
        public void Rejects_NoQuantity()
        {
            Assert.False(_validator.Validate(
                new UpdateMealPlanEntryCommand(Guid.NewGuid(), MealType.Dinner)).IsValid);
        }

        [Fact]
        public void Rejects_BothQuantities()
        {
            Assert.False(_validator.Validate(
                new UpdateMealPlanEntryCommand(Guid.NewGuid(), MealType.Dinner, Portions: 1m, Grams: 50m)).IsValid);
        }
    }

    public class RangeValidator
    {
        private readonly GetMealPlanRangeQueryValidator _validator = new();

        [Fact]
        public void Accepts_SingleDay()
        {
            Assert.True(_validator.Validate(new GetMealPlanRangeQuery(Day, Day)).IsValid);
        }

        [Fact]
        public void Accepts_FullMonthGrid()
        {
            // 42 days inclusive is exactly the month grid and must stay inside the bound.
            Assert.True(_validator.Validate(new GetMealPlanRangeQuery(Day, Day.AddDays(41))).IsValid);
        }

        [Fact]
        public void Rejects_RangeBeyondMaximum()
        {
            Assert.False(_validator.Validate(new GetMealPlanRangeQuery(Day, Day.AddDays(42))).IsValid);
        }

        [Fact]
        public void Rejects_InvertedRange()
        {
            Assert.False(_validator.Validate(new GetMealPlanRangeQuery(Day, Day.AddDays(-1))).IsValid);
        }
    }

    public class DeleteValidator
    {
        private readonly DeleteMealPlanEntryCommandValidator _validator = new();

        [Fact]
        public void Accepts_NonEmptyId()
        {
            Assert.True(_validator.Validate(new DeleteMealPlanEntryCommand(Guid.NewGuid())).IsValid);
        }

        [Fact]
        public void Rejects_EmptyId()
        {
            Assert.False(_validator.Validate(new DeleteMealPlanEntryCommand(Guid.Empty)).IsValid);
        }
    }
}
