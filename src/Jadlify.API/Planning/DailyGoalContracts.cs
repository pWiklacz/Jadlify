using Jadlify.Application.Planning;

namespace Jadlify.API.Planning;

/// <summary>Replaces the current user's single daily macro goal with the supplied targets.</summary>
public sealed record UpsertDailyGoalRequest(
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbohydrates);

/// <summary>
/// The current daily macro goal as a singleton resource. The MVP exposes one current goal
/// per user with no history or identity, so the response carries only the four target values.
/// </summary>
public sealed record DailyGoalResponse(
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbohydrates)
{
    public static DailyGoalResponse FromDto(PlanningMacroGoalDto dto) =>
        new(dto.Calories, dto.Protein, dto.Fat, dto.Carbohydrates);
}
