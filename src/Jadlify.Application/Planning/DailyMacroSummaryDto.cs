using Jadlify.Application.Recipes;

namespace Jadlify.Application.Planning;

public sealed record DailyMacroSummaryDto(
    DateOnly Date,
    IReadOnlyList<MealEntryMacroDto> Entries,
    RecipeMacroSummaryDto Total,
    PlanningMacroGoalDto? Goal,
    MacroRemainingDto? Remaining);
