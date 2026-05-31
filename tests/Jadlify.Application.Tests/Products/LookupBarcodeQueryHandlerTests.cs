using Jadlify.Application.Products;
using Jadlify.Application.Products.LookupBarcode;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Products;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Products;

public class LookupBarcodeQueryHandlerTests
{
    private const string Barcode = "3017624010701";

    [Fact]
    public async Task HandleAsync_ReturnsAlreadyInCatalog_AndSkipsOff_WhenBarcodeOwned()
    {
        var existing = new Product(
            Guid.NewGuid(),
            "Nutella",
            new MacroNutrients(539m, 6.3m, 30.9m, 57.5m),
            Barcode);
        var repository = new FakeProductRepository(existing);
        var lookup = new FakeBarcodeProductLookup(new BarcodeProductData(Name: "Should not be used"));
        var handler = new LookupBarcodeQueryHandler(repository, lookup);

        Result<BarcodeLookupResult> result = await handler.HandleAsync(
            new LookupBarcodeQuery(Barcode),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BarcodeLookupOutcome.AlreadyInCatalog, result.Value.Outcome);
        Assert.Equal(existing.Id, result.Value.ExistingProductId);
        Assert.Equal("Nutella", result.Value.Name);
        Assert.Equal(539m, result.Value.Calories);

        // Dedupe rule: an own-catalog hit must short-circuit before reaching OFF.
        Assert.Equal(0, lookup.CallCount);
    }

    [Fact]
    public async Task HandleAsync_ReturnsFound_WithMappedFields_WhenOffHasData()
    {
        var lookup = new FakeBarcodeProductLookup(
            new BarcodeProductData(
                Name: "Nutella",
                Brand: "Ferrero",
                Calories: 539m,
                Protein: 6.3m,
                Fat: 30.9m,
                Carbohydrates: 57.5m));
        var handler = new LookupBarcodeQueryHandler(new FakeProductRepository(), lookup);

        Result<BarcodeLookupResult> result = await handler.HandleAsync(
            new LookupBarcodeQuery(Barcode),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BarcodeLookupOutcome.Found, result.Value.Outcome);
        Assert.Equal(Barcode, result.Value.Barcode);
        Assert.Null(result.Value.ExistingProductId);
        Assert.Equal("Nutella", result.Value.Name);
        Assert.Equal("Ferrero", result.Value.Brand);
        Assert.Equal(539m, result.Value.Calories);
        Assert.Equal(1, lookup.CallCount);
    }

    [Fact]
    public async Task HandleAsync_ReturnsFound_WithBlanks_WhenOffDataPartial()
    {
        var lookup = new FakeBarcodeProductLookup(
            new BarcodeProductData(Name: "Mystery food", Protein: 8m));
        var handler = new LookupBarcodeQueryHandler(new FakeProductRepository(), lookup);

        Result<BarcodeLookupResult> result = await handler.HandleAsync(
            new LookupBarcodeQuery(Barcode),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BarcodeLookupOutcome.Found, result.Value.Outcome);
        Assert.Equal("Mystery food", result.Value.Name);
        Assert.Equal(8m, result.Value.Protein);
        Assert.Null(result.Value.Calories);
        Assert.Null(result.Value.Fat);
        Assert.Null(result.Value.Carbohydrates);
    }

    [Fact]
    public async Task HandleAsync_Found_CarriesPackageSizeAndExtendedFields()
    {
        var lookup = new FakeBarcodeProductLookup(
            new BarcodeProductData(
                Name: "Nutella",
                Calories: 539m,
                PackageSizeGrams: 400m,
                SaturatedFat: 10.6m,
                Sugars: 56.3m,
                Salt: 0.107m,
                VitaminD: 0.000005m));
        var handler = new LookupBarcodeQueryHandler(new FakeProductRepository(), lookup);

        Result<BarcodeLookupResult> result = await handler.HandleAsync(
            new LookupBarcodeQuery(Barcode),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BarcodeLookupOutcome.Found, result.Value.Outcome);
        Assert.Equal(400m, result.Value.PackageSizeGrams);
        Assert.Equal(10.6m, result.Value.SaturatedFat);
        Assert.Equal(56.3m, result.Value.Sugars);
        Assert.Equal(0.107m, result.Value.Salt);
        Assert.Equal(0.000005m, result.Value.VitaminD);
        Assert.Null(result.Value.Fiber);
    }

    [Fact]
    public async Task HandleAsync_AlreadyInCatalog_CarriesPackageSizeAndExtendedFields()
    {
        var existing = new Product(
            Guid.NewGuid(),
            "Nutella",
            new MacroNutrients(539m, 6.3m, 30.9m, 57.5m),
            Barcode,
            packageSizeGrams: 400m,
            details: new NutritionFacts(saturatedFat: 10.6m, sugars: 56.3m));
        var repository = new FakeProductRepository(existing);
        var handler = new LookupBarcodeQueryHandler(repository, new FakeBarcodeProductLookup());

        Result<BarcodeLookupResult> result = await handler.HandleAsync(
            new LookupBarcodeQuery(Barcode),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BarcodeLookupOutcome.AlreadyInCatalog, result.Value.Outcome);
        Assert.Equal(400m, result.Value.PackageSizeGrams);
        Assert.Equal(10.6m, result.Value.SaturatedFat);
        Assert.Equal(56.3m, result.Value.Sugars);
        Assert.Null(result.Value.Fiber);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WithBarcodeEchoed_WhenOffMisses()
    {
        var lookup = new FakeBarcodeProductLookup(result: null);
        var handler = new LookupBarcodeQueryHandler(new FakeProductRepository(), lookup);

        Result<BarcodeLookupResult> result = await handler.HandleAsync(
            new LookupBarcodeQuery(Barcode),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BarcodeLookupOutcome.NotFound, result.Value.Outcome);
        Assert.Equal(Barcode, result.Value.Barcode);
        Assert.Null(result.Value.ExistingProductId);
        Assert.Null(result.Value.Name);
    }
}
