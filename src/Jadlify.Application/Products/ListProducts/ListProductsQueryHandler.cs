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
        int skip = Math.Max(0, query.Skip ?? 0);
        int requestedTake = query.Take ?? ProductListBounds.DefaultTake;
        int take = Math.Clamp(requestedTake, 1, ProductListBounds.MaxTake);

        IReadOnlyList<Product> products = await _products.ListAsync(
            query.Search,
            skip,
            take,
            cancellationToken);
        IReadOnlyList<ProductDto> dtos = products.Select(ProductDto.FromDomain).ToList();

        return Result.Ok(dtos);
    }
}
