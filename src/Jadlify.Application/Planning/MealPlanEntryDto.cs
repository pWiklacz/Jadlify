using Jadlify.Domain.Planning;

namespace Jadlify.Application.Planning;

/// <summary>
/// Read model for a meal-plan entry on a selected day. Entries reference the current
/// recipe by id rather than snapshotting it, so the recipe name is resolved from the
/// owner-scoped recipe repository at read time and carried here for display. S-04 stops
/// at identity and display data; day-level macro totals belong to S-05.
/// </summary>
public sealed record MealPlanEntryDto(
    Guid Id,
    DateOnly Date,
    Guid RecipeId,
    string RecipeName,
    MealType MealType,
    int Portions)
{
    public static MealPlanEntryDto FromDomain(MealPlanEntry entry, string recipeName) =>
        new(
            entry.Id,
            entry.Date,
            entry.RecipeId,
            recipeName,
            entry.MealType,
            entry.Portions);
}
