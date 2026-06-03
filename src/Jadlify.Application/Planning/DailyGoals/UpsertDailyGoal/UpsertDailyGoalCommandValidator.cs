using FluentValidation;

namespace Jadlify.Application.Planning.DailyGoals.UpsertDailyGoal;

public sealed class UpsertDailyGoalCommandValidator : AbstractValidator<UpsertDailyGoalCommand>
{
    public UpsertDailyGoalCommandValidator()
    {
        RuleFor(x => x.Calories)
            .GreaterThan(0m)
            .LessThanOrEqualTo(PlanningValidationBounds.MaxDailyCalories);

        RuleFor(x => x.Protein)
            .InclusiveBetween(0m, PlanningValidationBounds.MaxDailyMacroGrams);

        RuleFor(x => x.Fat)
            .InclusiveBetween(0m, PlanningValidationBounds.MaxDailyMacroGrams);

        RuleFor(x => x.Carbohydrates)
            .InclusiveBetween(0m, PlanningValidationBounds.MaxDailyMacroGrams);
    }
}
