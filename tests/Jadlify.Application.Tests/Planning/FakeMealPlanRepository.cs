using Jadlify.Application.Planning;
using Jadlify.Domain.Planning;

namespace Jadlify.Application.Tests.Planning;

/// <summary>
/// In-memory <see cref="IMealPlanRepository"/> for handler unit tests. Mirrors the real
/// repository's owner-scoped, idempotent contract (update/delete are no-ops when the entry
/// is absent) but without persistence — the handlers under test contain no scoping logic.
/// Records update/delete calls so tests can assert delegation.
/// </summary>
internal sealed class FakeMealPlanRepository : IMealPlanRepository
{
    private readonly List<MealPlanEntry> _entries;

    public FakeMealPlanRepository(params MealPlanEntry[] seed)
    {
        _entries = [.. seed];
    }

    public IReadOnlyList<MealPlanEntry> Entries => _entries;

    public int UpdateCount { get; private set; }

    public List<Guid> DeletedIds { get; } = [];

    public Task AddAsync(MealPlanEntry entry, CancellationToken cancellationToken = default)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<MealPlanEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_entries.SingleOrDefault(entry => entry.Id == id));

    public Task<IReadOnlyList<MealPlanEntry>> ListByDateAsync(
        DateOnly date,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MealPlanEntry>>([.. _entries.Where(entry => entry.Date == date)]);

    public Task<IReadOnlyCollection<Guid>> ListUsedRecipeIdsAsync(
        IReadOnlyCollection<Guid> recipeIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Guid>>(
        [
            .. _entries
                .Select(entry => entry.RecipeId)
                .Where(recipeIds.Contains)
                .Distinct()
        ]);

    public Task UpdateAsync(MealPlanEntry entry, CancellationToken cancellationToken = default)
    {
        UpdateCount++;

        int index = _entries.FindIndex(existing => existing.Id == entry.Id);
        if (index >= 0)
        {
            _entries[index] = entry;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        DeletedIds.Add(id);
        _entries.RemoveAll(entry => entry.Id == id);
        return Task.CompletedTask;
    }
}
