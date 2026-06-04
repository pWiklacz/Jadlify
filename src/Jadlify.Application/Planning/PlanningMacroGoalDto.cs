using Jadlify.Domain.Planning;

namespace Jadlify.Application.Planning;

/// <summary>
/// Read model for the single current daily macro goal: the four target macro values.
/// The MVP exposes one current goal per user with no history or identity, so the goal
/// is surfaced as a singleton resource without an id.
/// </summary>
public sealed record PlanningMacroGoalDto(
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbohydrates)
{
    public static PlanningMacroGoalDto FromDomain(DailyMacroGoal goal) =>
        new(
            goal.Target.Calories,
            goal.Target.Protein,
            goal.Target.Fat,
            goal.Target.Carbohydrates);
}
