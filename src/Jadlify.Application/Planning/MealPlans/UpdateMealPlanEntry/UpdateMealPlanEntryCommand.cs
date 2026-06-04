using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Planning;

namespace Jadlify.Application.Planning.MealPlans.UpdateMealPlanEntry;

/// <summary>
/// Updates only the meal type and portions of an existing entry. The entry's date and
/// referenced recipe are intentionally not editable; changing those means delete and add.
/// </summary>
public sealed record UpdateMealPlanEntryCommand(
    Guid Id,
    MealType MealType,
    int Portions) : ICommand;
