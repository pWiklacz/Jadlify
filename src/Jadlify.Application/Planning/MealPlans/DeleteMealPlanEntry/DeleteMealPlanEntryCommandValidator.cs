using FluentValidation;

namespace Jadlify.Application.Planning.MealPlans.DeleteMealPlanEntry;

public sealed class DeleteMealPlanEntryCommandValidator : AbstractValidator<DeleteMealPlanEntryCommand>
{
    public DeleteMealPlanEntryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
