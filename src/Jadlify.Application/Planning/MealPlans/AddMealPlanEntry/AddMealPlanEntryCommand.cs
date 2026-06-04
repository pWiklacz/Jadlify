using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Planning;

namespace Jadlify.Application.Planning.MealPlans.AddMealPlanEntry;

/// <summary>Adds a recipe to a selected day and meal type with a positive integer portion count.</summary>
public sealed record AddMealPlanEntryCommand(
    DateOnly Date,
    Guid RecipeId,
    MealType MealType,
    int Portions) : ICommand<Guid>;
