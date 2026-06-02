using FluentValidation;

namespace Jadlify.Application.Recipes.UpdateRecipe;

public sealed class UpdateRecipeCommandValidator : AbstractValidator<UpdateRecipeCommand>
{
    public UpdateRecipeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

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
