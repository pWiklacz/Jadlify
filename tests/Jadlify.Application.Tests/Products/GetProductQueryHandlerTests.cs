using Jadlify.Application.Products;
using Jadlify.Application.Products.GetProduct;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Products;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Products;

public class GetProductQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsDto_WhenProductExists()
    {
        var id = Guid.NewGuid();
        var product = new Product(id, "Skyr", new MacroNutrients(63m, 11m, 0.2m, 4m));
        var handler = new GetProductQueryHandler(new FakeProductRepository(product));

        Result<ProductDto> result = await handler.HandleAsync(new GetProductQuery(id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal("Skyr", result.Value.Name);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenProductMissing()
    {
        var handler = new GetProductQueryHandler(new FakeProductRepository());

        Result<ProductDto> result = await handler.HandleAsync(
            new GetProductQuery(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Product.NotFound", result.Error.Code);
    }
}
