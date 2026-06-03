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
