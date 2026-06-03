using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Planning.MealPlans.DeleteMealPlanEntry;

/// <summary>Deletes one of the current user's meal-plan entries by id.</summary>
public sealed record DeleteMealPlanEntryCommand(Guid Id) : ICommand;
