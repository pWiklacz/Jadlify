using FluentValidation;

namespace Jadlify.Application.Planning.MealPlans.CopyMealPlanEntry;

public sealed class CopyMealPlanEntryCommandValidator : AbstractValidator<CopyMealPlanEntryCommand>
{
    public CopyMealPlanEntryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.TargetDates)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(MealPlanCopyRules.TargetDatesRequiredMessage)
            .Must(MealPlanCopyRules.IsWithinLimit)
            .WithMessage(MealPlanCopyRules.TargetDatesLimitMessage)
            .Must(MealPlanCopyRules.AreDistinct)
            .WithMessage(MealPlanCopyRules.TargetDatesDistinctMessage);
    }
}
