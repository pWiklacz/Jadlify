using Jadlify.Application.Common.Mediator;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Products.DeleteProduct;

public sealed class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand>
{
    private readonly IProductRepository _products;

    public DeleteProductCommandHandler(IProductRepository products)
    {
        _products = products;
    }

    // "Keep historical" policy: deletion of an owned product always succeeds;
    // the repository returns NotFound for another user's product.
    public Task<Result> HandleAsync(DeleteProductCommand command, CancellationToken cancellationToken) =>
        _products.DeleteAsync(command.Id, cancellationToken);
}
