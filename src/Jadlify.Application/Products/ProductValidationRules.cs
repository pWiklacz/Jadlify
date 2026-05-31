using System.Linq.Expressions;
using FluentValidation;

namespace Jadlify.Application.Products;

/// <summary>
/// Shared FluentValidation rules for the optional package size + extended per-100g
/// nutrient fields, applied identically by the create and update validators. Each rule
/// only fires when the field is present (it is nullable), so a blank field never fails —
/// manual entry stays optional (FR-006).
/// </summary>
public static class ProductValidationRules
{
    public static void ApplyExtendedNutritionRules<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, decimal?>> packageSizeSelector,
        params Expression<Func<T, decimal?>>[] nutrientSelectors)
    {
        validator.RuleFor(packageSizeSelector)
            .Must(value => value is null
                || value > 0m && value <= ProductValidationBounds.MaxPackageSizeGrams)
            .WithMessage(
                $"Package size must be greater than 0 and at most {ProductValidationBounds.MaxPackageSizeGrams} grams.");

        foreach (Expression<Func<T, decimal?>> selector in nutrientSelectors)
        {
            validator.RuleFor(selector)
                .Must(value => value is null
                    || value >= 0m && value <= ProductValidationBounds.MaxNutrientGramsPer100g)
                .WithMessage(
                    $"Value must be between 0 and {ProductValidationBounds.MaxNutrientGramsPer100g} grams per 100 g.");
        }
    }
}
