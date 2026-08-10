namespace Jadlify.Application.Planning;

/// <summary>
/// Shared validation bounds for planning commands. Kept in one place so the daily-goal
/// and meal-plan validators apply identical limits. Bounds are "minimal-plus-sane":
/// they reject negative or zero-calorie goals and obviously unrealistic upper values
/// without attempting nutritional coherence checks.
/// </summary>
public static class PlanningValidationBounds
{
    /// <summary>
    /// Practical upper bound for a daily calorie target. Generous enough for any athlete
    /// while still rejecting data-entry mistakes; the lower bound is enforced separately
    /// because calories must be strictly positive.
    /// </summary>
    public const decimal MaxDailyCalories = 20_000m;

    /// <summary>
    /// Upper bound for a daily macro gram target (protein, fat, carbohydrates). Far above
    /// any realistic target, so it only rejects obvious typos. Zero is allowed.
    /// </summary>
    public const decimal MaxDailyMacroGrams = 5_000m;

    /// <summary>Meal-plan recipe portions are positive half-steps with a sane per-entry ceiling.</summary>
    public const decimal MaxPortions = 100m;

    /// <summary>
    /// Per-entry ceiling for a direct product amount. Ten kilograms is far beyond any single
    /// planned serving, so it only rejects data-entry mistakes such as a misplaced decimal.
    /// </summary>
    public const decimal MaxEntryGrams = 10_000m;

    /// <summary>
    /// Largest inclusive planner range, in days. Six full weeks is exactly what the month
    /// grid renders, and bounding it here keeps one range read from degrading into a scan.
    /// </summary>
    public const int MaxRangeDays = 42;

    /// <summary>
    /// Largest number of target days a single copy operation may write to. Matched to
    /// <see cref="MaxRangeDays"/>: the month grid is the widest selection the planner can
    /// offer, so a request beyond it is not something a user assembled by hand.
    /// </summary>
    public const int MaxCopyTargetDays = MaxRangeDays;

    /// <summary>
    /// Ceiling on the entries a single copy operation may create, checked once the source is
    /// known. Target days alone do not bound the write — copying a dense day across six weeks
    /// multiplies out — so the product of the two is what has to stay bounded.
    /// </summary>
    public const int MaxCopyCreatedEntries = 500;
}
