using Jadlify.Application.Products;
using Jadlify.Application.Products.ListProducts;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Products;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Products;

public class ListProductsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_MapsProductsToDtos()
    {
        var product = new Product(
            Guid.NewGuid(),
            "Nutella",
            new MacroNutrients(539m, 6.3m, 30.9m, 57.5m),
            "3017624010701");
        var repository = new FakeProductRepository(product);
        var handler = new ListProductsQueryHandler(repository);

        Result<IReadOnlyList<ProductDto>> result = await handler.HandleAsync(
            new ListProductsQuery(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        ProductDto dto = Assert.Single(result.Value);
        Assert.Equal(product.Id, dto.Id);
        Assert.Equal("Nutella", dto.Name);
        Assert.Equal("3017624010701", dto.Barcode);
        Assert.Equal(539m, dto.Calories);
        Assert.Equal(6.3m, dto.Protein);
        Assert.Equal(30.9m, dto.Fat);
        Assert.Equal(57.5m, dto.Carbohydrates);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmptyList_WhenNoProducts()
    {
        var handler = new ListProductsQueryHandler(new FakeProductRepository());

        Result<IReadOnlyList<ProductDto>> result = await handler.HandleAsync(
            new ListProductsQuery(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
