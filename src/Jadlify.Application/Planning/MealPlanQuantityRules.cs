using Jadlify.Domain.Planning;

namespace Jadlify.Application.Planning;

/// <summary>
/// The quantity predicates shared by the add and update meal-plan validators, so both
/// surfaces reject the same values. These mirror the domain invariants on
/// <see cref="MealPlanEntry"/> and add the outer bounds that keep a typo from becoming a
/// plausible entry; the domain remains the last line of defence.
/// </summary>
internal static class MealPlanQuantityRules
{
    public const string PortionsMessage =
        "Portions must be a positive multiple of 0.5 and no more than 100.";

    public const string GramsMessage =
        "Grams must be a positive amount of no more than 10000.";

    public static bool IsValidPortions(decimal? portions) =>
        portions is { } value
        && value > 0m
        && value <= PlanningValidationBounds.MaxPortions
        && value % MealPlanEntry.PortionStep == 0m;

    public static bool IsValidGrams(decimal? grams) =>
        grams is { } value
        && value > 0m
        && value <= PlanningValidationBounds.MaxEntryGrams;
}
