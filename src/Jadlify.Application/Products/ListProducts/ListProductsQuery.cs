using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Products.ListProducts;

public sealed record ListProductsQuery : IQuery<IReadOnlyList<ProductDto>>;
