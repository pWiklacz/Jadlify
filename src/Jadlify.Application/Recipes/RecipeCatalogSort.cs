namespace Jadlify.Application.Recipes;

/// <summary>
/// The deterministic sort orders the paginated recipe catalog offers. Names are the
/// stable wire values; the boundary parses them and defaults to <see cref="NameAsc"/>.
/// A "recently updated" order is intentionally out of scope — recipes carry no
/// modification timestamp (same constraint as <see cref="Products.ProductCatalogSort"/>).
/// </summary>
public enum RecipeCatalogSort
{
    /// <summary>Alphabetically by name (A–Z). The default.</summary>
    NameAsc,

    /// <summary>Ascending calories per serving, then by name.</summary>
    CaloriesPerServingAsc,
}
