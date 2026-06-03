using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Planning;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Planning.DailyGoals.GetDailyGoal;

public sealed class GetDailyGoalQueryHandler : IQueryHandler<GetDailyGoalQuery, PlanningMacroGoalDto?>
{
    private readonly IDailyMacroGoalRepository _goals;

    public GetDailyGoalQueryHandler(IDailyMacroGoalRepository goals)
    {
        _goals = goals;
    }

    public async Task<Result<PlanningMacroGoalDto?>> HandleAsync(
        GetDailyGoalQuery query,
        CancellationToken cancellationToken)
    {
        DailyMacroGoal? goal = await _goals.GetCurrentAsync(cancellationToken);

        // A missing goal is a normal empty state, not a failure: the meal plan works
        // without a configured goal, so the query returns a success with a null value.
        return goal is null
            ? Result.Ok<PlanningMacroGoalDto?>(null)
            : Result.Ok<PlanningMacroGoalDto?>(PlanningMacroGoalDto.FromDomain(goal));
    }
}
