using FluentValidation;

namespace Jadlify.Application.Products.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
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
    }
}
