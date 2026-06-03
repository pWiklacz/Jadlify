using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Planning.MealPlans.ListMealPlanEntries;

/// <summary>Lists the current user's meal-plan entries for a single selected day.</summary>
public sealed record ListMealPlanEntriesQuery(DateOnly Date) : IQuery<IReadOnlyList<MealPlanEntryDto>>;
