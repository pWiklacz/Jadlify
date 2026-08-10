using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Recipes.GetRecipeCatalog;

/// <summary>
/// Requests one owner-scoped page of the recipe catalog. <see cref="Search"/> matches the
/// recipe name; <see cref="Skip"/>/<see cref="Take"/> are clamped by the handler.
/// </summary>
public sealed record GetRecipeCatalogQuery(
    string? Search = null,
    RecipeCatalogSort Sort = RecipeCatalogSort.NameAsc,
    int? Skip = null,
    int? Take = null) : IQuery<RecipeCatalogPageDto>;
