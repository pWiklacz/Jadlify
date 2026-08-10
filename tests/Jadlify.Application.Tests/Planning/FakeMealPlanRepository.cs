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

    public int AddRangeCount { get; private set; }

    public int ReplaceDaysCount { get; private set; }

    public List<DateOnly> ClearedDates { get; } = [];

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

    public Task<IReadOnlyList<MealPlanEntry>> ListByDateRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MealPlanEntry>>(
        [
            .. _entries
                .Where(entry => entry.Date >= from && entry.Date <= to)
                .OrderBy(entry => entry.Date)
                .ThenBy(entry => entry.Id)
        ]);

    public Task<IReadOnlyCollection<Guid>> ListUsedRecipeIdsAsync(
        IReadOnlyCollection<Guid> recipeIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Guid>>(
        [
            .. _entries
                .Where(entry => entry.RecipeId is { } recipeId && recipeIds.Contains(recipeId))
                .Select(entry => entry.RecipeId!.Value)
                .Distinct()
        ]);

    public Task AddRangeAsync(
        IReadOnlyCollection<MealPlanEntry> entries,
        CancellationToken cancellationToken = default)
    {
        AddRangeCount++;
        _entries.AddRange(entries);
        return Task.CompletedTask;
    }

    public Task ReplaceDaysAsync(
        IReadOnlyCollection<DateOnly> datesToClear,
        IReadOnlyCollection<MealPlanEntry> entries,
        CancellationToken cancellationToken = default)
    {
        ReplaceDaysCount++;
        ClearedDates.AddRange(datesToClear);

        // The real repository clears and inserts in one transaction; the fake keeps the same
        // all-or-nothing shape so a handler test cannot pass against a half-applied batch.
        _entries.RemoveAll(entry => datesToClear.Contains(entry.Date));
        _entries.AddRange(entries);
        return Task.CompletedTask;
    }

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
