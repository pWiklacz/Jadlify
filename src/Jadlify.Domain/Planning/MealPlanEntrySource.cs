namespace Jadlify.Domain.Planning;

/// <summary>
/// Which of the two source variants a <see cref="MealPlanEntry"/> carries. Members use
/// stable English names because both the wire contract and the persisted discriminator
/// key off them; the UI maps each to a Polish label.
/// </summary>
public enum MealPlanEntrySource
{
    /// <summary>
    /// The entry references one of the user's recipes and its quantity is a portion count.
    /// The recipe stays live: editing it changes future calculations for this entry.
    /// </summary>
    Recipe,

    /// <summary>
    /// The entry carries an owned <see cref="PlannedProductSnapshot"/> and its quantity is
    /// a gram amount. The snapshot is frozen at plan time, so later catalog edits or a
    /// product deletion never rewrite what was planned.
    /// </summary>
    Product,
}
