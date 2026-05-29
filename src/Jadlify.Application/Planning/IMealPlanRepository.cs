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

    Task UpdateAsync(MealPlanEntry entry, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
