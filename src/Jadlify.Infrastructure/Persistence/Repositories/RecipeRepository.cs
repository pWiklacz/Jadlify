using Jadlify.Application.Identity;
using Jadlify.Application.Recipes;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Jadlify.Infrastructure.Persistence.Repositories;

internal sealed class RecipeRepository : IRecipeRepository
{
    private static readonly Error NotFound =
        Error.NotFound("Recipe.NotFound", "The recipe was not found for the current user.");

    private static readonly Error InUse =
        Error.Conflict("Recipe.InUse", "The recipe is used by a meal-plan entry and cannot be deleted.");

    private readonly JadlifyDbContext _context;
    private readonly ICurrentUser _currentUser;

    public RecipeRepository(JadlifyDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        _context.Recipes.Add(recipe);
        _context.Entry(recipe).Property(PersistenceConstants.UserIdProperty).CurrentValue =
            _currentUser.UserId.Value;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        string owner = _currentUser.UserId.Value;

        return await _context.Recipes
            .Include(recipe => recipe.Ingredients)
            .SingleOrDefaultAsync(
                recipe => recipe.Id == id
                    && EF.Property<string>(recipe, PersistenceConstants.UserIdProperty) == owner,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Recipe>> ListAsync(CancellationToken cancellationToken = default)
    {
        string owner = _currentUser.UserId.Value;

        return await _context.Recipes
            .Include(recipe => recipe.Ingredients)
            .Where(recipe => EF.Property<string>(recipe, PersistenceConstants.UserIdProperty) == owner)
            .ToListAsync(cancellationToken);
    }

    public async Task<RecipeCatalogResult> GetCatalogAsync(
        string? search,
        RecipeCatalogSort sort,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        string owner = _currentUser.UserId.Value;

        IQueryable<Recipe> query = _context.Recipes
            .Where(recipe => EF.Property<string>(recipe, PersistenceConstants.UserIdProperty) == owner);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string normalizedSearch = search.Trim().ToLowerInvariant();
            query = query.Where(recipe => recipe.Name.ToLower().Contains(normalizedSearch));
        }

        // Count all matches before paging so the caller can render totals / page controls.
        int total = await query.CountAsync(cancellationToken);

        // Ordering happens in the database so it applies across the whole result set, not just
        // the fetched window — sorting a name-ordered page in memory would order the wrong rows.
        // The per-serving key repeats the macro formula as a *sort key only*; every displayed
        // value still comes from MacroCalculator over the loaded aggregate, which stays the one
        // deterministic macro core. Dividing by 100 is a constant positive factor and is folded
        // away here, so the key is proportional to calories per serving.
        query = sort switch
        {
            RecipeCatalogSort.CaloriesPerServingAsc => query
                .OrderBy(recipe =>
                    recipe.Ingredients.Sum(i => i.Per100Grams.Calories * i.WholeRecipeAmount.Value)
                    / recipe.Portions)
                .ThenBy(recipe => recipe.Name)
                .ThenBy(recipe => recipe.Id),
            _ => query
                .OrderBy(recipe => recipe.Name)
                .ThenBy(recipe => recipe.Id),
        };

        List<Recipe> items = await query
            .Include(recipe => recipe.Ingredients)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return new RecipeCatalogResult(items, total);
    }

    public async Task<IReadOnlyList<Recipe>> ListByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
        {
            return [];
        }

        string owner = _currentUser.UserId.Value;

        // Owner-scoped IN-filter: only the current user's recipes for the requested ids are
        // returned, so meal-plan listing can never surface another user's recipe. Ingredients
        // are not included because callers only need current display data (id and name).
        return await _context.Recipes
            .Where(recipe => ids.Contains(recipe.Id)
                && EF.Property<string>(recipe, PersistenceConstants.UserIdProperty) == owner)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Recipe>> ListByIdsWithIngredientsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
        {
            return [];
        }

        string owner = _currentUser.UserId.Value;

        return await _context.Recipes
            .Include(recipe => recipe.Ingredients)
            .Where(recipe => ids.Contains(recipe.Id)
                && EF.Property<string>(recipe, PersistenceConstants.UserIdProperty) == owner)
            .ToListAsync(cancellationToken);
    }

    public async Task<Result> UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        string owner = _currentUser.UserId.Value;

        Recipe? existing = await _context.Recipes
            .Include(candidate => candidate.Ingredients)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == recipe.Id
                    && EF.Property<string>(candidate, PersistenceConstants.UserIdProperty) == owner,
                cancellationToken);
        if (existing is null)
        {
            return Result.Fail(NotFound);
        }

        existing.ReplaceDetails(recipe.Name, recipe.Portions, recipe.Ingredients);

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        string owner = _currentUser.UserId.Value;

        Recipe? existing = await _context.Recipes.SingleOrDefaultAsync(
            recipe => recipe.Id == id
                && EF.Property<string>(recipe, PersistenceConstants.UserIdProperty) == owner,
            cancellationToken);
        if (existing is null)
        {
            return Result.Fail(NotFound);
        }

        bool inUse = await _context.MealPlanEntries.AnyAsync(
            entry => entry.RecipeId == id
                && EF.Property<string>(entry, PersistenceConstants.UserIdProperty) == owner,
            cancellationToken);
        if (inUse)
        {
            return Result.Fail(InUse);
        }

        _context.Recipes.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
