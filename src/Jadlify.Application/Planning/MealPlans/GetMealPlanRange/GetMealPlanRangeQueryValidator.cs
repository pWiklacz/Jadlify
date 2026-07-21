using FluentValidation;

namespace Jadlify.Application.Planning.MealPlans.GetMealPlanRange;

public sealed class GetMealPlanRangeQueryValidator : AbstractValidator<GetMealPlanRangeQuery>
{
    public GetMealPlanRangeQueryValidator()
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .WithMessage("The range end must not be before its start.");

        // The window is bounded so one range read can never turn into an unbounded scan.
        // 42 days is exactly the month grid; anything larger is a client paging its own way.
        RuleFor(x => x)
            .Must(query => query.To.DayNumber - query.From.DayNumber < PlanningValidationBounds.MaxRangeDays)
            .When(query => query.To >= query.From)
            .WithMessage($"The range must cover at most {PlanningValidationBounds.MaxRangeDays} days.")
            .OverridePropertyName("To");
    }
}
