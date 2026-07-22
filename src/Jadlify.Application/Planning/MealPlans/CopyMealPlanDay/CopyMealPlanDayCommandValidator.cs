using FluentValidation;

namespace Jadlify.Application.Planning.MealPlans.CopyMealPlanDay;

public sealed class CopyMealPlanDayCommandValidator : AbstractValidator<CopyMealPlanDayCommand>
{
    public CopyMealPlanDayCommandValidator()
    {
        RuleFor(x => x.Mode)
            .IsInEnum();

        RuleFor(x => x.TargetDates)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(MealPlanCopyRules.TargetDatesRequiredMessage)
            .Must(MealPlanCopyRules.IsWithinLimit)
            .WithMessage(MealPlanCopyRules.TargetDatesLimitMessage)
            .Must(MealPlanCopyRules.AreDistinct)
            .WithMessage(MealPlanCopyRules.TargetDatesDistinctMessage);

        // Under Replace this would clear the source day and refill it from what was just read,
        // and under Add it would silently double it. Neither is a plausible intent.
        RuleFor(x => x)
            .Must(command => !command.TargetDates.Contains(command.SourceDate))
            .When(command => command.TargetDates is { Count: > 0 })
            .WithMessage("The source day must not be one of the target days.")
            .OverridePropertyName(nameof(CopyMealPlanDayCommand.TargetDates));
    }
}
