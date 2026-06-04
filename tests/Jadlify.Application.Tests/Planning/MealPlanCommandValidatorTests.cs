using Jadlify.Application.Planning.MealPlans.AddMealPlanEntry;
using Jadlify.Application.Planning.MealPlans.DeleteMealPlanEntry;
using Jadlify.Application.Planning.MealPlans.UpdateMealPlanEntry;
using Jadlify.Domain.Planning;

namespace Jadlify.Application.Tests.Planning;

public class MealPlanCommandValidatorTests
{
    private static readonly DateOnly Day = new(2026, 6, 2);

    public class AddValidator
    {
        private readonly AddMealPlanEntryCommandValidator _validator = new();

        private static AddMealPlanEntryCommand Valid(
            Guid? recipeId = null,
            MealType mealType = MealType.Breakfast,
            int portions = 1) =>
            new(Day, recipeId ?? Guid.NewGuid(), mealType, portions);

        [Fact]
        public void Accepts_ValidEntry()
        {
            Assert.True(_validator.Validate(Valid()).IsValid);
        }

        [Fact]
        public void Rejects_EmptyRecipeId()
        {
            Assert.False(_validator.Validate(Valid(recipeId: Guid.Empty)).IsValid);
        }

        [Fact]
        public void Rejects_ZeroPortions()
        {
            Assert.False(_validator.Validate(Valid(portions: 0)).IsValid);
        }

        [Fact]
        public void Rejects_NegativePortions()
        {
            Assert.False(_validator.Validate(Valid(portions: -1)).IsValid);
        }

        [Fact]
        public void Rejects_UnrealisticPortions()
        {
            Assert.False(_validator.Validate(Valid(portions: 1000)).IsValid);
        }

        [Fact]
        public void Rejects_UndefinedMealType()
        {
            Assert.False(_validator.Validate(Valid(mealType: (MealType)99)).IsValid);
        }
    }

    public class UpdateValidator
    {
        private readonly UpdateMealPlanEntryCommandValidator _validator = new();

        private static UpdateMealPlanEntryCommand Valid(
            Guid? id = null,
            MealType mealType = MealType.Dinner,
            int portions = 2) =>
            new(id ?? Guid.NewGuid(), mealType, portions);

        [Fact]
        public void Accepts_ValidUpdate()
        {
            Assert.True(_validator.Validate(Valid()).IsValid);
        }

        [Fact]
        public void Rejects_EmptyId()
        {
            Assert.False(_validator.Validate(Valid(id: Guid.Empty)).IsValid);
        }

        [Fact]
        public void Rejects_ZeroPortions()
        {
            Assert.False(_validator.Validate(Valid(portions: 0)).IsValid);
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
