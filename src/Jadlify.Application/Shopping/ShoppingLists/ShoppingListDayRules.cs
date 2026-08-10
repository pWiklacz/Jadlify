using Jadlify.Application.Planning;

namespace Jadlify.Application.Shopping.ShoppingLists;

/// <summary>
/// The source-day predicates for creating a shopping list. Days are an arbitrary selection, but
/// still bounded: distinct, capped in count, and confined to a 42-day span so one projection
/// read stays bounded — the same window the planner's month grid covers.
/// </summary>
internal static class ShoppingListDayRules
{
    public const string DaysRequiredMessage = "Select at least one day to build the list from.";

    public static readonly string DaysCountMessage =
        $"A shopping list may draw from at most {PlanningValidationBounds.MaxRangeDays} days.";

    public const string DaysDistinctMessage = "The selected days must not repeat.";

    public static readonly string DaysSpanMessage =
        $"The selected days must fall within a {PlanningValidationBounds.MaxRangeDays}-day span.";

    public static bool IsWithinCount(IReadOnlyList<DateOnly>? days) =>
        days is not null && days.Count <= PlanningValidationBounds.MaxRangeDays;

    public static bool AreDistinct(IReadOnlyList<DateOnly>? days) =>
        days is null || days.Distinct().Count() == days.Count;

    /// <summary>
    /// Bounds the window the days fall in, independent of how many were selected: a handful of
    /// days chosen months apart would still force an unbounded range read.
    /// </summary>
    public static bool IsWithinSpan(IReadOnlyList<DateOnly>? days) =>
        days is null || days.Count == 0 ||
        days.Max().DayNumber - days.Min().DayNumber < PlanningValidationBounds.MaxRangeDays;
}
