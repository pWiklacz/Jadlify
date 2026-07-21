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

        // Exactly one unit must be supplied here. Whether it is the right unit for the target
        // entry depends on the stored entry, so that check belongs to the handler.
        RuleFor(x => x)
            .Must(command => (command.Portions is not null) ^ (command.Grams is not null))
            .WithMessage("Provide exactly one quantity: portions for a recipe entry, or grams for a product entry.")
            .OverridePropertyName("Quantity");

        When(x => x.Portions is not null, () =>
            RuleFor(x => x.Portions)
                .Must(MealPlanQuantityRules.IsValidPortions)
                .WithMessage(MealPlanQuantityRules.PortionsMessage));

        When(x => x.Grams is not null, () =>
            RuleFor(x => x.Grams)
                .Must(MealPlanQuantityRules.IsValidGrams)
                .WithMessage(MealPlanQuantityRules.GramsMessage));
    }
}
