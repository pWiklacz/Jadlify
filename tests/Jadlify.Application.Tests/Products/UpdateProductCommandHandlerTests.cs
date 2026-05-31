using Jadlify.Application.Products.UpdateProduct;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Products;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Products;

public class UpdateProductCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_UpdatesExistingProduct()
    {
        var id = Guid.NewGuid();
        var existing = new Product(id, "Old name", new MacroNutrients(100m, 5m, 2m, 10m), "12345678");
        var repository = new FakeProductRepository(existing);
        var handler = new UpdateProductCommandHandler(repository);
        var command = new UpdateProductCommand(id, "New name", "87654321", 200m, 10m, 4m, 20m);

        Result result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Product updated = Assert.Single(repository.Products);
        Assert.Equal("New name", updated.Name);
        Assert.Equal("87654321", updated.Barcode);
        Assert.Equal(200m, updated.Per100Grams.Calories);
    }

    [Fact]
    public async Task HandleAsync_PropagatesNotFound_WhenProductMissing()
    {
        var repository = new FakeProductRepository();
        var handler = new UpdateProductCommandHandler(repository);
        var command = new UpdateProductCommand(Guid.NewGuid(), "Ghost", null, 100m, 5m, 2m, 10m);

        Result result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Product.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_UpdatesPackageSizeAndExtendedFields()
    {
        var id = Guid.NewGuid();
        var existing = new Product(id, "Old name", new MacroNutrients(100m, 5m, 2m, 10m), "12345678");
        var repository = new FakeProductRepository(existing);
        var handler = new UpdateProductCommandHandler(repository);
        var command = new UpdateProductCommand(id, "New name", "12345678", 200m, 10m, 4m, 20m)
        {
            PackageSizeGrams = 250m,
            SaturatedFat = 3.5m,
            Sugars = 18m,
        };

        Result result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Product updated = Assert.Single(repository.Products);
        Assert.Equal(250m, updated.PackageSizeGrams);
        Assert.Equal(3.5m, updated.Details.SaturatedFat);
        Assert.Equal(18m, updated.Details.Sugars);
        Assert.Null(updated.Details.Fiber);
    }
}
