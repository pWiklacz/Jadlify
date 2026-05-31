using Jadlify.Application.Products.UpdateProduct;

namespace Jadlify.Application.Tests.Products;

public class UpdateProductCommandValidatorTests
{
    private readonly UpdateProductCommandValidator _validator = new();

    private static UpdateProductCommand Valid(
        Guid? id = null,
        string name = "Nutella",
        string? barcode = "3017624010701",
        decimal calories = 539m,
        decimal protein = 6.3m,
        decimal fat = 30.9m,
        decimal carbohydrates = 57.5m) =>
        new(id ?? Guid.NewGuid(), name, barcode, calories, protein, fat, carbohydrates);

    [Fact]
    public void Accepts_RealisticData()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    [Fact]
    public void Rejects_EmptyId()
    {
        Assert.False(_validator.Validate(Valid(id: Guid.Empty)).IsValid);
    }

    [Fact]
    public void Rejects_EmptyName()
    {
        Assert.False(_validator.Validate(Valid(name: "")).IsValid);
    }

    [Fact]
    public void Rejects_ImpossibleCarbohydrates()
    {
        Assert.False(_validator.Validate(Valid(carbohydrates: 150m)).IsValid);
    }

    [Fact]
    public void Rejects_MalformedBarcode()
    {
        Assert.False(_validator.Validate(Valid(barcode: "abc")).IsValid);
    }
}
