using Jadlify.Application.Products.CreateProduct;
using Jadlify.Domain.Products;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Products;

public class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_AddsProductAndReturnsNewId()
    {
        var repository = new FakeProductRepository();
        var handler = new CreateProductCommandHandler(repository);
        var command = new CreateProductCommand("Nutella", "3017624010701", 539m, 6.3m, 30.9m, 57.5m);

        Result<Guid> result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        Product added = Assert.Single(repository.Products);
        Assert.Equal(result.Value, added.Id);
        Assert.Equal("Nutella", added.Name);
        Assert.Equal("3017624010701", added.Barcode);
        Assert.Equal(539m, added.Per100Grams.Calories);
        Assert.Equal(6.3m, added.Per100Grams.Protein);
        Assert.Equal(30.9m, added.Per100Grams.Fat);
        Assert.Equal(57.5m, added.Per100Grams.Carbohydrates);
    }

    [Fact]
    public async Task HandleAsync_NormalizesBlankBarcodeToNull()
    {
        var repository = new FakeProductRepository();
        var handler = new CreateProductCommandHandler(repository);
        var command = new CreateProductCommand("Manual entry", "   ", 100m, 5m, 2m, 10m);

        Result<Guid> result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Product added = Assert.Single(repository.Products);
        Assert.Null(added.Barcode);
    }

    [Fact]
    public async Task HandleAsync_PersistsPackageSizeAndExtendedFields()
    {
        var repository = new FakeProductRepository();
        var handler = new CreateProductCommandHandler(repository);
        var command = new CreateProductCommand("Nutella", "3017624010701", 539m, 6.3m, 30.9m, 57.5m)
        {
            PackageSizeGrams = 400m,
            SaturatedFat = 10.6m,
            Sugars = 56.3m,
            Fiber = 0m,
            Salt = 0.107m,
            VitaminD = 0.000005m,
        };

        Result<Guid> result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Product added = Assert.Single(repository.Products);
        Assert.Equal(400m, added.PackageSizeGrams);
        Assert.Equal(10.6m, added.Details.SaturatedFat);
        Assert.Equal(56.3m, added.Details.Sugars);
        Assert.Equal(0m, added.Details.Fiber);
        Assert.Equal(0.107m, added.Details.Salt);
        Assert.Equal(0.000005m, added.Details.VitaminD);
        // Fields the command left null stay null on the stored product.
        Assert.Null(added.Details.Potassium);
    }
}
