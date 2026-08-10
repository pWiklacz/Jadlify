using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Recipes;

/// <summary>
/// Owner-scoped persistence for user recipes. Reads include recipe ingredients so
/// deterministic macro calculations can run against the ingredient snapshots in
/// an owner-scoped graph.
/// </summary>
public interface IRecipeRepository
{
    Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default);

    /// <summary>Loads the recipe including its ingredients, scoped to the current user.</summary>
    Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Recipe>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads one owner-scoped page of the recipe catalog plus the total match count.
    /// Ingredients are included because the shared macro core computes the page's summary
    /// totals from them; the window is bounded, so at most <c>take</c> compositions load.
    /// </summary>
    Task<RecipeCatalogResult> GetCatalogAsync(
        string? search,
        RecipeCatalogSort sort,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the current user's recipes for the requested ids. Used by meal-plan listing
    /// to resolve current recipe display data in one batch; must filter by current user,
    /// return only requested ids, and never expose another user's recipes.
    /// </summary>
    Task<IReadOnlyList<Recipe>> ListByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the current user's recipes for deterministic macro calculations.
    /// Includes ingredients and their macro snapshots; must filter by current user,
    /// return only requested ids, and never expose another user's recipes.
    /// </summary>
    Task<IReadOnlyList<Recipe>> ListByIdsWithIngredientsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the complete recipe aggregate for the current user, including
    /// ingredient composition and product macro snapshots.
    /// </summary>
    Task<Result> UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the recipe when it is not referenced by any meal-plan entry.
    /// Returns a failure result instead of cascading dependent user data.
    /// </summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
