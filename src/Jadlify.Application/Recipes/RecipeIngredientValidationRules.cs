using System.Linq.Expressions;
using FluentValidation;

namespace Jadlify.Application.Recipes;

internal static class RecipeIngredientValidationRules
{
    public static IRuleBuilderOptions<T, IReadOnlyList<RecipeIngredientInput>> ApplyRecipeIngredientRules<T>(
        this IRuleBuilder<T, IReadOnlyList<RecipeIngredientInput>> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .Must(HaveUniqueProductIds)
            .WithMessage("Recipe ingredients must not contain duplicate products.");

    public static void ApplyRecipeIngredientElementRules<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, IEnumerable<RecipeIngredientInput>>> selector)
    {
        validator.RuleForEach(selector)
            .ChildRules(ingredient =>
            {
                ingredient.RuleFor(x => x.ProductId)
                    .NotEmpty();

                ingredient.RuleFor(x => x.WholeRecipeGrams)
                    .InclusiveBetween(0.01m, RecipeValidationBounds.MaxWholeRecipeGrams);
            });
    }

    private static bool HaveUniqueProductIds(IReadOnlyList<RecipeIngredientInput>? ingredients) =>
        ingredients is null ||
        ingredients.Select(ingredient => ingredient.ProductId).Distinct().Count() == ingredients.Count;
}
