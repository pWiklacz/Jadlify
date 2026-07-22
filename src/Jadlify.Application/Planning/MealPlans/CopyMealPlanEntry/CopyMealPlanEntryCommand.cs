using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Planning.MealPlans.CopyMealPlanEntry;

/// <summary>
/// Copies one entry onto each date in <see cref="TargetDates"/>, leaving the original where it
/// is. Each copy gets a new id and keeps the source's meal type, source variant, and quantity.
/// Copying onto the entry's own date is allowed — that is how a meal is duplicated within a day.
/// </summary>
public sealed record CopyMealPlanEntryCommand(
    Guid Id,
    IReadOnlyList<DateOnly> TargetDates) : ICommand<IReadOnlyList<CreatedMealPlanEntryDto>>;
