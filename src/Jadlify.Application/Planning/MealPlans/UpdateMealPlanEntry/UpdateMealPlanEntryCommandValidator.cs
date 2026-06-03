using FluentValidation;

namespace Jadlify.Application.Planning.MealPlans.UpdateMealPlanEntry;

public sealed class UpdateMealPlanEntryCommandValidator : AbstractValidator<UpdateMealPlanEntryCommand>
{
    public UpdateMealPlanEntryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.MealType)
            .IsInEnum();

        RuleFor(x => x.Portions)
            .InclusiveBetween(1, PlanningValidationBounds.MaxPortions);
    }
}
