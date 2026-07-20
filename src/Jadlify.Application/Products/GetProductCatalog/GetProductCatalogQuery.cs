using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Products;

namespace Jadlify.Application.Products.GetProductCatalog;

/// <summary>
/// Requests one owner-scoped page of the product catalog. <see cref="Category"/> and
/// <see cref="UncategorizedOnly"/> are mutually exclusive category filters — set neither for
/// "all", <see cref="Category"/> for a specific bucket, or <see cref="UncategorizedOnly"/> to
/// return only products with no category. <see cref="Skip"/>/<see cref="Take"/> are clamped by
/// the handler.
/// </summary>
public sealed record GetProductCatalogQuery(
    string? Search = null,
    ProductCategory? Category = null,
    bool UncategorizedOnly = false,
    ProductCatalogSort Sort = ProductCatalogSort.NameAsc,
    int? Skip = null,
    int? Take = null) : IQuery<ProductCatalogPageDto>;
