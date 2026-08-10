namespace Jadlify.Application.Planning;

/// <summary>
/// How a copied day meets whatever the target day already holds. The choice is explicit
/// because the two outcomes are not recoverable from each other: <see cref="Add"/> can be
/// undone by deleting the copies, <see cref="Replace"/> discards what was there.
/// </summary>
public enum MealPlanDayCopyMode
{
    /// <summary>Appends the copies, keeping every entry already planned on the target day.</summary>
    Add = 0,

    /// <summary>Clears the target day's existing entries and leaves only the copies.</summary>
    Replace = 1
}
