using FluentValidation;

namespace Jadlify.Application.Planning.MealPlans.MoveMealPlanEntry;

public sealed class MoveMealPlanEntryCommandValidator : AbstractValidator<MoveMealPlanEntryCommand>
{
    public MoveMealPlanEntryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.MealType)
            .IsInEnum();
    }
}
