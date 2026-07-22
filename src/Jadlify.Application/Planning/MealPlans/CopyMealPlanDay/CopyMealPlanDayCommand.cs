using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Planning.MealPlans.CopyMealPlanDay;

/// <summary>
/// Copies every entry planned on <see cref="SourceDate"/> onto each date in
/// <see cref="TargetDates"/>. <see cref="Mode"/> decides whether the copies join the target
/// day's existing entries or replace them. The source day must not appear among the targets:
/// copying a day onto itself either doubles it or, under Replace, rewrites it with itself.
/// </summary>
public sealed record CopyMealPlanDayCommand(
    DateOnly SourceDate,
    IReadOnlyList<DateOnly> TargetDates,
    MealPlanDayCopyMode Mode) : ICommand<IReadOnlyList<CreatedMealPlanEntryDto>>;
