using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Products;

namespace Jadlify.Domain.Tests.Nutrition;

public class NutritionFactsTests
{
    [Fact]
    public void Empty_HasAllNullFields()
    {
        NutritionFacts facts = NutritionFacts.Empty;

        Assert.Null(facts.SaturatedFat);
        Assert.Null(facts.MonounsaturatedFat);
        Assert.Null(facts.PolyunsaturatedFat);
        Assert.Null(facts.TransFat);
        Assert.Null(facts.Sugars);
        Assert.Null(facts.Fiber);
        Assert.Null(facts.Salt);
        Assert.Null(facts.Sodium);
        Assert.Null(facts.Potassium);
        Assert.Null(facts.Calcium);
        Assert.Null(facts.Iron);
        Assert.Null(facts.VitaminA);
        Assert.Null(facts.VitaminC);
        Assert.Null(facts.VitaminD);
    }

    [Fact]
    public void Constructor_AcceptsNullsAndNonNegativeValues()
    {
        var facts = new NutritionFacts(
            saturatedFat: 12.5m,
            sugars: 0m,
            salt: 0.107m,
            // Vitamins/minerals are grams, hence sub-milligram values.
            vitaminD: 0.000005m);

        Assert.Equal(12.5m, facts.SaturatedFat);
        Assert.Equal(0m, facts.Sugars);
        Assert.Equal(0.107m, facts.Salt);
        Assert.Equal(0.000005m, facts.VitaminD);
        Assert.Null(facts.Fiber);
        Assert.Null(facts.Potassium);
    }

    [Fact]
    public void Constructor_RejectsNegativeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NutritionFacts(saturatedFat: -1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NutritionFacts(sugars: -0.1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NutritionFacts(sodium: -5m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NutritionFacts(vitaminD: -0.0001m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Product_RejectsNonPositivePackageSize(int packageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Product(Guid.NewGuid(), "Oats", new MacroNutrients(100m, 10m, 5m, 20m), packageSizeGrams: packageSize));
    }

    [Fact]
    public void Product_StoresPackageSizeAndDetails()
    {
        var details = new NutritionFacts(saturatedFat: 8m, sugars: 56.3m);
        var product = new Product(
            Guid.NewGuid(),
            "Nutella",
            new MacroNutrients(539m, 6.3m, 30.9m, 57.5m),
            barcode: "3017624010701",
            packageSizeGrams: 400m,
            details: details);

        Assert.Equal(400m, product.PackageSizeGrams);
        Assert.Equal(8m, product.Details.SaturatedFat);
        Assert.Equal(56.3m, product.Details.Sugars);
    }

    [Fact]
    public void Product_DefaultsDetailsToEmpty_WhenNotProvided()
    {
        var product = new Product(Guid.NewGuid(), "Oats", new MacroNutrients(100m, 10m, 5m, 20m));

        Assert.Equal(NutritionFacts.Empty, product.Details);
        Assert.Null(product.PackageSizeGrams);
    }
}
