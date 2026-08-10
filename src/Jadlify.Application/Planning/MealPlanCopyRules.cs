namespace Jadlify.Application.Planning;

/// <summary>
/// The target-day predicates shared by the copy-entry and copy-day validators, so both
/// surfaces bound a batch identically. Every rule here is checked before anything is written:
/// a copy that would be rejected halfway through must be rejected before it starts.
/// </summary>
internal static class MealPlanCopyRules
{
    public const string TargetDatesRequiredMessage =
        "Provide at least one target date to copy to.";

    public static readonly string TargetDatesLimitMessage =
        $"A copy may target at most {PlanningValidationBounds.MaxCopyTargetDays} days.";

    public const string TargetDatesDistinctMessage =
        "Target dates must not repeat.";

    public static bool IsWithinLimit(IReadOnlyList<DateOnly>? targetDates) =>
        targetDates is not null && targetDates.Count <= PlanningValidationBounds.MaxCopyTargetDays;

    /// <summary>
    /// A repeated target is a client bug, not a request to copy twice. Silently de-duplicating
    /// it would make the response's entry count disagree with what the caller asked for.
    /// </summary>
    public static bool AreDistinct(IReadOnlyList<DateOnly>? targetDates) =>
        targetDates is null || targetDates.Distinct().Count() == targetDates.Count;
}
