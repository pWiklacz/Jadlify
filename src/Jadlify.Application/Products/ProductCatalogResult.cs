using Jadlify.Domain.Products;

namespace Jadlify.Application.Products;

/// <summary>
/// Repository-level result of a catalog page query: the owner-scoped domain products for
/// the requested window plus the <see cref="Total"/> count of all matches. The handler maps
/// <see cref="Items"/> to <see cref="ProductDto"/> and echoes the paging window.
/// </summary>
public sealed record ProductCatalogResult(IReadOnlyList<Product> Items, int Total);
