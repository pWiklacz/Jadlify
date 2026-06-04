using FluentValidation;

namespace Jadlify.Application.Planning.MealPlans.AddMealPlanEntry;

public sealed class AddMealPlanEntryCommandValidator : AbstractValidator<AddMealPlanEntryCommand>
{
    public AddMealPlanEntryCommandValidator()
    {
        RuleFor(x => x.RecipeId)
            .NotEmpty();

        RuleFor(x => x.MealType)
            .IsInEnum();

        RuleFor(x => x.Portions)
            .InclusiveBetween(1, PlanningValidationBounds.MaxPortions);
    }
}
