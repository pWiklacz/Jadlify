using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Planning;

namespace Jadlify.Application.Planning.MealPlans.UpdateMealPlanEntry;

/// <summary>
/// Updates the meal type and quantity of an existing entry. The quantity must be supplied in
/// the unit matching the entry's own source — <see cref="Portions"/> for a recipe entry,
/// <see cref="Grams"/> for a product entry — and a mismatch is rejected rather than coerced.
/// The date and the source itself stay immutable; changing those means delete and add.
/// </summary>
public sealed record UpdateMealPlanEntryCommand(
    Guid Id,
    MealType MealType,
    decimal? Portions = null,
    decimal? Grams = null) : ICommand;
