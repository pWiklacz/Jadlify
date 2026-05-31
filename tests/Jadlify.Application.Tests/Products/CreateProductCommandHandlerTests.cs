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
}
