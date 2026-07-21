using Jadlify.Application.Recipes;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Recipes;

internal sealed class FakeRecipeRepository : IRecipeRepository
{
    private static readonly Error NotFound =
        Error.NotFound("Recipe.NotFound", "The recipe was not found for the current user.");

    private readonly List<Recipe> _recipes;

    public FakeRecipeRepository(params Recipe[] seed)
    {
        _recipes = [.. seed];
    }

    public IReadOnlyList<Recipe> Recipes => _recipes;

    public bool DeleteReturnsConflict { get; set; }

    public Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        _recipes.Add(recipe);
        return Task.CompletedTask;
    }

    public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_recipes.SingleOrDefault(recipe => recipe.Id == id));

    public Task<IReadOnlyList<Recipe>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Recipe>>([.. _recipes]);

    public Task<RecipeCatalogResult> GetCatalogAsync(
        string? search,
        RecipeCatalogSort sort,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Recipe> matches = _recipes;
        if (!string.IsNullOrWhiteSpace(search))
        {
            matches = matches.Where(recipe =>
                recipe.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        List<Recipe> ordered = [.. sort == RecipeCatalogSort.CaloriesPerServingAsc
            ? matches
                .OrderBy(recipe => MacroCalculator.RecipePerServing(recipe).Calories)
                .ThenBy(recipe => recipe.Name, StringComparer.Ordinal)
            : matches.OrderBy(recipe => recipe.Name, StringComparer.Ordinal)];

        return Task.FromResult(new RecipeCatalogResult(
            [.. ordered.Skip(skip).Take(take)],
            ordered.Count));
    }

    public Task<IReadOnlyList<Recipe>> ListByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Recipe>>([.. _recipes.Where(recipe => ids.Contains(recipe.Id))]);

    public Task<IReadOnlyList<Recipe>> ListByIdsWithIngredientsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Recipe>>([.. _recipes.Where(recipe => ids.Contains(recipe.Id))]);

    public Task<Result> UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        int index = _recipes.FindIndex(existing => existing.Id == recipe.Id);
        if (index < 0)
        {
            return Task.FromResult(Result.Fail(NotFound));
        }

        _recipes[index] = recipe;
        return Task.FromResult(Result.Ok());
    }

    public Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (DeleteReturnsConflict)
        {
            return Task.FromResult(Result.Fail(
                Error.Conflict("Recipe.InUse", "The recipe is used by a meal-plan entry and cannot be deleted.")));
        }

        int removed = _recipes.RemoveAll(recipe => recipe.Id == id);
        return Task.FromResult(removed > 0 ? Result.Ok() : Result.Fail(NotFound));
    }
}
