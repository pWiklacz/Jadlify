using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Planning;

namespace Jadlify.Application.Planning.MealPlans.MoveMealPlanEntry;

/// <summary>
/// Reschedules one entry to <see cref="Date"/> under <see cref="MealType"/>. The entry keeps
/// its id, source, and quantity — this is the named alternative to deleting and re-adding,
/// which would lose the identity anything else refers to.
/// </summary>
public sealed record MoveMealPlanEntryCommand(
    Guid Id,
    DateOnly Date,
    MealType MealType) : ICommand;
