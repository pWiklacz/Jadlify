using Jadlify.Domain.Planning;

namespace Jadlify.Application.Planning;

/// <summary>
/// Owner-scoped persistence for meal-plan entries. Listing by date supports the
/// daily flow and future daily-summary / shopping-list projections; the recipes and
/// products needed for deterministic calculation are loaded through the recipe and
/// product repositories so every query stays owner-scoped with no cross-user joins.
/// </summary>
public interface IMealPlanRepository
{
    Task AddAsync(MealPlanEntry entry, CancellationToken cancellationToken = default);

    Task<MealPlanEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MealPlanEntry>> ListByDateAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the current user's entries for an inclusive date window in one read. Backs the
    /// day, week, and month planner views, so a 42-day grid never degrades into a query per
    /// day. Callers bound the window before calling; the repository does not re-check it.
    /// </summary>
    Task<IReadOnlyList<MealPlanEntry>> ListByDateRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the subset of <paramref name="recipeIds"/> that the current user has planned
    /// at least one meal for. One batched read backs the catalog's "W PLANIE" badge, so the
    /// index never issues a usage query per recipe. Must filter by current user.
    /// </summary>
    Task<IReadOnlyCollection<Guid>> ListUsedRecipeIdsAsync(
        IReadOnlyCollection<Guid> recipeIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds every entry in <paramref name="entries"/> in one write. Batch copies go through
    /// here rather than a loop over <see cref="AddAsync"/> so a failure anywhere leaves no
    /// entries behind at all — a half-copied week is worse than a rejected request.
    /// </summary>
    Task AddRangeAsync(
        IReadOnlyCollection<MealPlanEntry> entries,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the current user's entries on <paramref name="datesToClear"/> and adds
    /// <paramref name="entries"/> in the same write, so the target days are never observably
    /// empty. Only owner-scoped rows are removed; another user's entries on the same dates are
    /// untouched. Callers validate the whole request before calling — the repository writes
    /// what it is given.
    /// </summary>
    Task ReplaceDaysAsync(
        IReadOnlyCollection<DateOnly> datesToClear,
        IReadOnlyCollection<MealPlanEntry> entries,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(MealPlanEntry entry, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
