using FluentValidation.Results;
using Jadlify.Application.Products.CreateProduct;
using Jadlify.Domain.Products;

namespace Jadlify.Application.Tests.Products;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    private static CreateProductCommand Valid(
        string name = "Nutella",
        string? barcode = "3017624010701",
        decimal calories = 539m,
        decimal protein = 6.3m,
        decimal fat = 30.9m,
        decimal carbohydrates = 57.5m) =>
        new(name, barcode, calories, protein, fat, carbohydrates);

    [Fact]
    public void Accepts_RealisticData()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    [Fact]
    public void Accepts_MissingBarcode()
    {
        Assert.True(_validator.Validate(Valid(barcode: null)).IsValid);
    }

    [Fact]
    public void Rejects_EmptyName()
    {
        Assert.False(_validator.Validate(Valid(name: "")).IsValid);
    }

    [Fact]
    public void Rejects_NameOverMaxLength()
    {
        Assert.False(_validator.Validate(Valid(name: new string('x', 201))).IsValid);
    }

    [Fact]
    public void Rejects_ImpossibleCalories()
    {
        ValidationResult result = _validator.Validate(Valid(calories: 9000m));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductCommand.Calories));
    }

    [Fact]
    public void Rejects_ImpossibleProtein()
    {
        ValidationResult result = _validator.Validate(Valid(protein: 150m));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductCommand.Protein));
    }

    [Fact]
    public void Rejects_NegativeMacro()
    {
        Assert.False(_validator.Validate(Valid(fat: -1m)).IsValid);
    }

    [Theory]
    [InlineData("1234567")]      // too short (7)
    [InlineData("123456789012345")] // too long (15)
    [InlineData("12345abc")]     // non-digit
    public void Rejects_MalformedBarcode(string barcode)
    {
        Assert.False(_validator.Validate(Valid(barcode: barcode)).IsValid);
    }

    [Theory]
    [InlineData("12345678")]       // EAN-8
    [InlineData("3017624010701")]  // EAN-13
    public void Accepts_WellShapedBarcode(string barcode)
    {
        Assert.True(_validator.Validate(Valid(barcode: barcode)).IsValid);
    }

    [Fact]
    public void Accepts_RealisticExtendedFieldsAndPackageSize()
    {
        CreateProductCommand command = Valid() with
        {
            PackageSizeGrams = 400m,
            SaturatedFat = 10.6m,
            Sugars = 56.3m,
            Fiber = 0m,
            Salt = 0.107m,
            // Sub-milligram vitamin value (stored in grams) must pass.
            VitaminD = 0.000005m,
        };

        Assert.True(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Rejects_NegativeExtendedField()
    {
        CreateProductCommand command = Valid() with { Sugars = -1m };

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Rejects_ExtendedFieldOverBound()
    {
        // > 100 g per 100 g is physically impossible.
        CreateProductCommand command = Valid() with { SaturatedFat = 150m };

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData(0)]        // must be strictly positive
    [InlineData(-5)]
    [InlineData(200_000)]  // beyond the ~100 kg ceiling
    public void Rejects_OutOfRangePackageSize(int packageSize)
    {
        CreateProductCommand command = Valid() with { PackageSizeGrams = packageSize };

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Accepts_BrandAndCategory()
    {
        CreateProductCommand command = Valid() with
        {
            Brand = "Ferrero",
            Category = ProductCategory.PantryAndDryGoods,
        };

        Assert.True(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Accepts_MissingBrandAndCategory()
    {
        // A null category is the valid "Bez kategorii" state; a blank brand is optional.
        CreateProductCommand command = Valid() with { Brand = null, Category = null };

        Assert.True(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Rejects_BrandOverMaxLength()
    {
        CreateProductCommand command = Valid() with { Brand = new string('x', 201) };

        ValidationResult result = _validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductCommand.Brand));
    }
}
