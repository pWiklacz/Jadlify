using Jadlify.Domain.Recipes;

namespace Jadlify.Application.Recipes;

/// <summary>
/// Repository-level result of a catalog page query: the owner-scoped domain recipes for
/// the requested window (ingredients included so the shared macro core can compute the
/// summary totals) plus the <see cref="Total"/> count of all matches.
/// </summary>
public sealed record RecipeCatalogResult(IReadOnlyList<Recipe> Items, int Total);
