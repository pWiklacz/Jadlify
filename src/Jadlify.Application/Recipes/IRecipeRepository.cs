using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Recipes;

/// <summary>
/// Owner-scoped persistence for user recipes. Reads include recipe ingredients so
/// deterministic macro calculations can run against an owner-scoped graph; product
/// nutrient values are loaded through <see cref="Products.IProductRepository"/>.
/// </summary>
public interface IRecipeRepository
{
    Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default);

    /// <summary>Loads the recipe including its ingredients, scoped to the current user.</summary>
    Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Recipe>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists recipe metadata (name, portions) for the current user. Ingredient
    /// composition reconciliation is out of scope for the F-02 foundation.
    /// </summary>
    Task<Result> UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the recipe when it is not referenced by any meal-plan entry.
    /// Returns a failure result instead of cascading dependent user data.
    /// </summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
