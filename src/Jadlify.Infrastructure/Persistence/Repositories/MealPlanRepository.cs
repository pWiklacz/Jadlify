using Jadlify.Application.Identity;
using Jadlify.Application.Planning;
using Jadlify.Domain.Planning;
using Microsoft.EntityFrameworkCore;

namespace Jadlify.Infrastructure.Persistence.Repositories;

internal sealed class MealPlanRepository : IMealPlanRepository
{
    private readonly JadlifyDbContext _context;
    private readonly ICurrentUser _currentUser;

    public MealPlanRepository(JadlifyDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task AddAsync(MealPlanEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _context.MealPlanEntries.Add(entry);
        _context.Entry(entry).Property(PersistenceConstants.UserIdProperty).CurrentValue =
            _currentUser.UserId.Value;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<MealPlanEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        string owner = _currentUser.UserId.Value;

        return await _context.MealPlanEntries.SingleOrDefaultAsync(
            entry => entry.Id == id
                && EF.Property<string>(entry, PersistenceConstants.UserIdProperty) == owner,
            cancellationToken);
    }

    public async Task<IReadOnlyList<MealPlanEntry>> ListByDateAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        string owner = _currentUser.UserId.Value;

        return await _context.MealPlanEntries
            .Where(entry => entry.Date == date
                && EF.Property<string>(entry, PersistenceConstants.UserIdProperty) == owner)
            .OrderBy(entry => entry.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MealPlanEntry>> ListByDateRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        string owner = _currentUser.UserId.Value;

        // One owner-scoped read over the whole window, served by the (user_id, date) index.
        // Ordering by date then id gives a stable sequence across repeat reads; the meal-type
        // ordering inside a day is applied in the application, where the enum order is known
        // (the column stores names, so sorting it here would be alphabetical, not meal order).
        return await _context.MealPlanEntries
            .Where(entry => entry.Date >= from
                && entry.Date <= to
                && EF.Property<string>(entry, PersistenceConstants.UserIdProperty) == owner)
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> ListUsedRecipeIdsAsync(
        IReadOnlyCollection<Guid> recipeIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recipeIds);

        if (recipeIds.Count == 0)
        {
            return [];
        }

        string owner = _currentUser.UserId.Value;

        // One owner-scoped IN-filter over the requested ids, projected distinct: the catalog
        // resolves usage for the whole page in a single round-trip instead of per recipe.
        // Product entries have a null recipe_id and are filtered out by the IN-filter itself.
        return await _context.MealPlanEntries
            .Where(entry => entry.RecipeId != null
                && recipeIds.Contains(entry.RecipeId.Value)
                && EF.Property<string>(entry, PersistenceConstants.UserIdProperty) == owner)
            .Select(entry => entry.RecipeId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(
        IReadOnlyCollection<MealPlanEntry> entries,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);

        if (entries.Count == 0)
        {
            return;
        }

        AddOwned(entries);

        // One SaveChanges for the whole batch: EF wraps the inserts in a single transaction,
        // so a constraint violation on any copy rolls back every other copy with it.
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceDaysAsync(
        IReadOnlyCollection<DateOnly> datesToClear,
        IReadOnlyCollection<MealPlanEntry> entries,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(datesToClear);
        ArgumentNullException.ThrowIfNull(entries);

        string owner = _currentUser.UserId.Value;

        // Owner-scoped read of everything being cleared: another user's entries on the same
        // dates are never loaded, so they can never be removed.
        List<MealPlanEntry> existing = datesToClear.Count == 0
            ? []
            : await _context.MealPlanEntries
                .Where(entry => datesToClear.Contains(entry.Date)
                    && EF.Property<string>(entry, PersistenceConstants.UserIdProperty) == owner)
                .ToListAsync(cancellationToken);

        _context.MealPlanEntries.RemoveRange(existing);
        AddOwned(entries);

        // The removals and the inserts are staged together and committed by one SaveChanges, so
        // the target days are never observably empty and a failure leaves the day as it was.
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(MealPlanEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        string owner = _currentUser.UserId.Value;

        MealPlanEntry? existing = await _context.MealPlanEntries.SingleOrDefaultAsync(
            candidate => candidate.Id == entry.Id
                && EF.Property<string>(candidate, PersistenceConstants.UserIdProperty) == owner,
            cancellationToken);
        if (existing is null)
        {
            return;
        }

        _context.Entry(existing).CurrentValues.SetValues(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        string owner = _currentUser.UserId.Value;

        MealPlanEntry? existing = await _context.MealPlanEntries.SingleOrDefaultAsync(
            entry => entry.Id == id
                && EF.Property<string>(entry, PersistenceConstants.UserIdProperty) == owner,
            cancellationToken);
        if (existing is null)
        {
            return;
        }

        _context.MealPlanEntries.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // Stages entries as the current user's without saving, so a caller can compose several
    // changes into one transaction. The owner is a shadow property, hence the per-entry set.
    private void AddOwned(IReadOnlyCollection<MealPlanEntry> entries)
    {
        string owner = _currentUser.UserId.Value;

        foreach (MealPlanEntry entry in entries)
        {
            _context.MealPlanEntries.Add(entry);
            _context.Entry(entry).Property(PersistenceConstants.UserIdProperty).CurrentValue = owner;
        }
    }
}
