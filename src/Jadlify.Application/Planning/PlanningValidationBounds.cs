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

    /// <summary>Meal-plan portions are positive integers with a sane per-entry ceiling.</summary>
    public const int MaxPortions = 100;
}
