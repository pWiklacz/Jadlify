using FluentValidation;

namespace Jadlify.Application.Planning.MealPlans.AddMealPlanEntry;

public sealed class AddMealPlanEntryCommandValidator : AbstractValidator<AddMealPlanEntryCommand>
{
    public AddMealPlanEntryCommandValidator()
    {
        RuleFor(x => x.MealType)
            .IsInEnum();

        // Exactly one source. Both variants present is as much a client error as neither:
        // an ambiguous request must never be silently resolved to one of the two.
        RuleFor(x => x)
            .Must(command => command.IsRecipeSource ^ command.IsProductSource)
            .WithMessage("Provide exactly one source: recipeId with portions, or productId with grams.")
            .OverridePropertyName("Source");

        When(x => x.IsRecipeSource && !x.IsProductSource, () =>
        {
            RuleFor(x => x.RecipeId)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .NotEqual(Guid.Empty);

            RuleFor(x => x.Portions)
                .Must(MealPlanQuantityRules.IsValidPortions)
                .WithMessage(MealPlanQuantityRules.PortionsMessage);
        });

        When(x => x.IsProductSource && !x.IsRecipeSource, () =>
        {
            RuleFor(x => x.ProductId)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .NotEqual(Guid.Empty);

            RuleFor(x => x.Grams)
                .Must(MealPlanQuantityRules.IsValidGrams)
                .WithMessage(MealPlanQuantityRules.GramsMessage);
        });
    }
}
