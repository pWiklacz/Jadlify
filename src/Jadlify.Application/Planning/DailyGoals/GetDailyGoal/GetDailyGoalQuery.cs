using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Planning.DailyGoals.GetDailyGoal;

/// <summary>Reads the current user's single daily macro goal, or null when none is configured.</summary>
public sealed record GetDailyGoalQuery : IQuery<PlanningMacroGoalDto?>;
