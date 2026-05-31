using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Products;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Products.GetProduct;

public sealed class GetProductQueryHandler : IQueryHandler<GetProductQuery, ProductDto>
{
    private static readonly Error NotFound =
        Error.NotFound("Product.NotFound", "The product was not found for the current user.");

    private readonly IProductRepository _products;

    public GetProductQueryHandler(IProductRepository products)
    {
        _products = products;
    }

    public async Task<Result<ProductDto>> HandleAsync(GetProductQuery query, CancellationToken cancellationToken)
    {
        Product? product = await _products.GetByIdAsync(query.Id, cancellationToken);

        return product is null
            ? Result.Fail<ProductDto>(NotFound)
            : Result.Ok(ProductDto.FromDomain(product));
    }
}
