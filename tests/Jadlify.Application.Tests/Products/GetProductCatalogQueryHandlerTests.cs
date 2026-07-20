using Jadlify.Application.Products;
using Jadlify.Application.Products.GetProductCatalog;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Products;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Products;

public class GetProductCatalogQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPage_WithTotalAndEchoedWindow()
    {
        FakeProductRepository repository = new(
            NewProduct("Banana", ProductCategory.Fruits),
            NewProduct("Apple", ProductCategory.Fruits),
            NewProduct("Cucumber", ProductCategory.Vegetables));
        GetProductCatalogQueryHandler handler = new(repository);

        Result<ProductCatalogPageDto> result = await handler.HandleAsync(
            new GetProductCatalogQuery(Skip: 0, Take: 2),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Total counts all matches, ignoring the page window.
        Assert.Equal(3, result.Value.Total);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(0, result.Value.Skip);
        Assert.Equal(2, result.Value.Take);
    }

    [Fact]
    public async Task Handle_ClampsTakeToMax()
    {
        FakeProductRepository repository = new(NewProduct("Apple", ProductCategory.Fruits));
        GetProductCatalogQueryHandler handler = new(repository);

        Result<ProductCatalogPageDto> result = await handler.HandleAsync(
            new GetProductCatalogQuery(Take: 10_000),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProductListBounds.MaxTake, result.Value.Take);
    }

    [Fact]
    public async Task Handle_FiltersToUncategorized_WhenRequested()
    {
        FakeProductRepository repository = new(
            NewProduct("Apple", ProductCategory.Fruits),
            NewProduct("Mystery", category: null));
        GetProductCatalogQueryHandler handler = new(repository);

        Result<ProductCatalogPageDto> result = await handler.HandleAsync(
            new GetProductCatalogQuery(UncategorizedOnly: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        ProductDto only = Assert.Single(result.Value.Items);
        Assert.Equal("Mystery", only.Name);
        Assert.Null(only.Category);
    }

    private static Product NewProduct(string name, ProductCategory? category) =>
        new(Guid.NewGuid(), name, new MacroNutrients(100m, 1m, 1m, 1m), category: category);
}
