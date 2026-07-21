using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Planning.MealPlans.GetMealPlanRange;

/// <summary>
/// Reads an inclusive window of planned days in one round-trip. The window backs the day,
/// week, and month planner views alike, so the month grid costs one request rather than 42.
/// </summary>
public sealed record GetMealPlanRangeQuery(DateOnly From, DateOnly To) : IQuery<MealPlanRangeDto>;
