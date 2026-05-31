using Jadlify.Application.Products.DeleteProduct;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Products;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Products;

public class DeleteProductCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_RemovesExistingProduct()
    {
        var id = Guid.NewGuid();
        var existing = new Product(id, "Doomed", new MacroNutrients(100m, 5m, 2m, 10m));
        var repository = new FakeProductRepository(existing);
        var handler = new DeleteProductCommandHandler(repository);

        Result result = await handler.HandleAsync(new DeleteProductCommand(id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(repository.Products);
    }

    [Fact]
    public async Task HandleAsync_PropagatesNotFound_WhenProductMissing()
    {
        var repository = new FakeProductRepository();
        var handler = new DeleteProductCommandHandler(repository);

        Result result = await handler.HandleAsync(new DeleteProductCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
