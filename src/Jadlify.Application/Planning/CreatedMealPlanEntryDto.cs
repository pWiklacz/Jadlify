using Jadlify.Domain.Planning;

namespace Jadlify.Application.Planning;

/// <summary>
/// An entry a copy operation created, identified well enough for the caller to locate it
/// without re-reading the range: the new id and the day it landed on. Batch copies return one
/// of these per created entry, in target-date order.
/// </summary>
public sealed record CreatedMealPlanEntryDto(Guid Id, DateOnly Date, MealType MealType);
