using Jadlify.Domain.Planning;

namespace Jadlify.Application.Planning;

/// <summary>
/// Owner-scoped persistence for the single current daily macro goal per user.
/// No goal history is exposed in the MVP.
/// </summary>
public interface IDailyMacroGoalRepository
{
    Task<DailyMacroGoal?> GetCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>Stores the goal as the current target for the user, replacing any existing one.</summary>
    Task UpsertAsync(DailyMacroGoal goal, CancellationToken cancellationToken = default);
}
