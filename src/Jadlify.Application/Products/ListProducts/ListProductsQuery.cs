using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Products.ListProducts;

public sealed record ListProductsQuery(
    string? Search = null,
    int? Skip = null,
    int? Take = null) : IQuery<IReadOnlyList<ProductDto>>;
