namespace Jadlify.Application.Products;

/// <summary>
/// The deterministic sort orders the paginated product catalog offers. Names are the
/// stable wire values; the boundary parses them and defaults to <see cref="NameAsc"/>.
/// A "recently updated" order is intentionally out of scope — products carry no
/// modification timestamp.
/// </summary>
public enum ProductCatalogSort
{
    /// <summary>Alphabetically by name (A–Z). The default.</summary>
    NameAsc,

    /// <summary>Ascending calories per 100 g, then by name.</summary>
    CaloriesAsc,

    /// <summary>Grouped by category (uncategorized last), then by name.</summary>
    Category,
}
