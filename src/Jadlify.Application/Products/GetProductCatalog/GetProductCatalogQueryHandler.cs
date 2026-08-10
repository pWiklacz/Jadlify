using Jadlify.Application.Common.Mediator;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Products.GetProductCatalog;

public sealed class GetProductCatalogQueryHandler
    : IQueryHandler<GetProductCatalogQuery, ProductCatalogPageDto>
{
    private readonly IProductRepository _products;

    public GetProductCatalogQueryHandler(IProductRepository products)
    {
        _products = products;
    }

    public async Task<Result<ProductCatalogPageDto>> HandleAsync(
        GetProductCatalogQuery query,
        CancellationToken cancellationToken)
    {
        int skip = Math.Max(0, query.Skip ?? 0);
        int requestedTake = query.Take ?? ProductListBounds.DefaultTake;
        int take = Math.Clamp(requestedTake, 1, ProductListBounds.MaxTake);

        ProductCatalogResult page = await _products.GetCatalogAsync(
            query.Search,
            query.Category,
            query.UncategorizedOnly,
            query.Sort,
            skip,
            take,
            cancellationToken);

        IReadOnlyList<ProductDto> items = page.Items.Select(ProductDto.FromDomain).ToList();

        return Result.Ok(new ProductCatalogPageDto(items, page.Total, skip, take));
    }
}
