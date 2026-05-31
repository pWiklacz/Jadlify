using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Products;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Products.ListProducts;

public sealed class ListProductsQueryHandler : IQueryHandler<ListProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly IProductRepository _products;

    public ListProductsQueryHandler(IProductRepository products)
    {
        _products = products;
    }

    public async Task<Result<IReadOnlyList<ProductDto>>> HandleAsync(
        ListProductsQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Product> products = await _products.ListAsync(cancellationToken);
        IReadOnlyList<ProductDto> dtos = products.Select(ProductDto.FromDomain).ToList();

        return Result.Ok(dtos);
    }
}
