using Jadlify.Application.Recipes;

namespace Jadlify.Application.Planning;

/// <summary>
/// One day inside a planner range: its entries, per-entry macros, the day total, and the
/// current goal with the signed amount still remaining. <see cref="Remaining"/> is negative
/// once the day is over target, so a consumer can show "over by" without recomputing.
/// Goal and remaining are <c>null</c> together when no goal is configured.
/// </summary>
public sealed record MealPlanDayDto(
    DateOnly Date,
    IReadOnlyList<MealPlanEntryDto> Entries,
    IReadOnlyList<MealEntryMacroDto> EntryMacros,
    RecipeMacroSummaryDto Total,
    PlanningMacroGoalDto? Goal,
    MacroRemainingDto? Remaining);

/// <summary>
/// A bounded, inclusive window of planned days. Every date from <see cref="From"/> to
/// <see cref="To"/> appears in <see cref="Days"/> in ascending order, including days with no
/// entries — the month grid renders 42 cells and must not have to infer the gaps.
/// </summary>
public sealed record MealPlanRangeDto(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<MealPlanDayDto> Days);
