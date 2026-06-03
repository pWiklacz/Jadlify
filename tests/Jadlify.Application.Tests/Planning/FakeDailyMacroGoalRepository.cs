using Jadlify.Application.Planning;
using Jadlify.Domain.Planning;

namespace Jadlify.Application.Tests.Planning;

/// <summary>
/// In-memory <see cref="IDailyMacroGoalRepository"/> for handler unit tests. Models the
/// singleton "one current goal per user" contract: upsert replaces the stored goal.
/// </summary>
internal sealed class FakeDailyMacroGoalRepository : IDailyMacroGoalRepository
{
    private DailyMacroGoal? _current;

    public FakeDailyMacroGoalRepository(DailyMacroGoal? seed = null)
    {
        _current = seed;
    }

    public DailyMacroGoal? Current => _current;

    public int UpsertCount { get; private set; }

    public Task<DailyMacroGoal?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_current);

    public Task UpsertAsync(DailyMacroGoal goal, CancellationToken cancellationToken = default)
    {
        _current = goal;
        UpsertCount++;
        return Task.CompletedTask;
    }
}
