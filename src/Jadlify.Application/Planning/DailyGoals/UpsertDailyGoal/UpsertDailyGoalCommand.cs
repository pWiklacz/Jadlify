using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Planning.DailyGoals.UpsertDailyGoal;

/// <summary>Replaces the current user's single daily macro goal with the supplied targets.</summary>
public sealed record UpsertDailyGoalCommand(
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbohydrates) : ICommand;
