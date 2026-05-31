using FluentValidation;

namespace Jadlify.Application.Products.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(ProductValidationBounds.MaxNameLength);

        RuleFor(x => x.Calories)
            .InclusiveBetween(0m, ProductValidationBounds.MaxCaloriesPer100g);

        RuleFor(x => x.Protein)
            .InclusiveBetween(0m, ProductValidationBounds.MaxMacroGramsPer100g);

        RuleFor(x => x.Fat)
            .InclusiveBetween(0m, ProductValidationBounds.MaxMacroGramsPer100g);

        RuleFor(x => x.Carbohydrates)
            .InclusiveBetween(0m, ProductValidationBounds.MaxMacroGramsPer100g);

        When(x => !string.IsNullOrEmpty(x.Barcode), () =>
            RuleFor(x => x.Barcode!)
                .Matches(ProductValidationBounds.BarcodePattern)
                .WithMessage("Barcode must be 8 to 14 digits."));

        this.ApplyExtendedNutritionRules(
            x => x.PackageSizeGrams,
            x => x.SaturatedFat,
            x => x.MonounsaturatedFat,
            x => x.PolyunsaturatedFat,
            x => x.TransFat,
            x => x.Sugars,
            x => x.Fiber,
            x => x.Salt,
            x => x.Sodium,
            x => x.Potassium,
            x => x.Calcium,
            x => x.Iron,
            x => x.VitaminA,
            x => x.VitaminC,
            x => x.VitaminD);
    }
}
