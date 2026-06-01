using FluentValidation;

namespace Jadlify.Application.Recipes.CreateRecipe;

public sealed class CreateRecipeCommandValidator : AbstractValidator<CreateRecipeCommand>
{
    public CreateRecipeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(RecipeValidationBounds.MaxNameLength);

        RuleFor(x => x.Portions)
            .InclusiveBetween(1, RecipeValidationBounds.MaxPortions);

        RuleFor(x => x.Ingredients)
            .ApplyRecipeIngredientRules();

        this.ApplyRecipeIngredientElementRules(x => x.Ingredients);
    }
}
