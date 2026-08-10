using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Recipes;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Planning.MealPlans.GetMealPlanRange;

/// <summary>
/// Builds a whole planner window from three reads, no matter how many days it spans: one
/// owner-scoped entry read over the range, one batched recipe read for the distinct recipes
/// those entries reference, and one goal read. Nothing here loops a per-day handler, so a
/// 42-day month costs the same round-trips as a single day.
/// </summary>
public sealed class GetMealPlanRangeQueryHandler : IQueryHandler<GetMealPlanRangeQuery, MealPlanRangeDto>
{
    private readonly IMealPlanRepository _mealPlans;
    private readonly IRecipeRepository _recipes;
    private readonly IDailyMacroGoalRepository _goals;

    public GetMealPlanRangeQueryHandler(
        IMealPlanRepository mealPlans,
        IRecipeRepository recipes,
        IDailyMacroGoalRepository goals)
    {
        _mealPlans = mealPlans;
        _recipes = recipes;
        _goals = goals;
    }

    public async Task<Result<MealPlanRangeDto>> HandleAsync(
        GetMealPlanRangeQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IReadOnlyList<MealPlanEntry> entries =
            await _mealPlans.ListByDateRangeAsync(query.From, query.To, cancellationToken);

        Guid[] recipeIds = MealPlanRecipeResolution.DistinctRecipeIds(entries);
        IReadOnlyList<Recipe> recipes = recipeIds.Length == 0
            ? []
            : await _recipes.ListByIdsWithIngredientsAsync(recipeIds, cancellationToken);
        var recipesById = recipes.ToDictionary(recipe => recipe.Id);
        var recipeNamesById = recipes.ToDictionary(recipe => recipe.Id, recipe => recipe.Name);

        DailyMacroGoal? goal = await _goals.GetCurrentAsync(cancellationToken);
        PlanningMacroGoalDto? goalDto = goal is null ? null : PlanningMacroGoalDto.FromDomain(goal);

        var entriesByDate = entries
            .GroupBy(entry => entry.Date)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<MealPlanEntry>)[.. group]);

        List<MealPlanDayDto> days = [];
        for (DateOnly date = query.From; date <= query.To; date = date.AddDays(1))
        {
            IReadOnlyList<MealPlanEntry> dayEntries =
                entriesByDate.GetValueOrDefault(date) ?? [];

            (IReadOnlyList<MealEntryMacroDto> entryMacros, MacroNutrients total) =
                MealPlanDayProjection.Build(dayEntries, recipesById);

            days.Add(new MealPlanDayDto(
                date,
                [.. dayEntries.Select(entry => MealPlanEntryDto.FromDomain(
                    entry,
                    entry.RecipeId is { } recipeId
                        ? recipeNamesById.GetValueOrDefault(recipeId, string.Empty)
                        : null))],
                entryMacros,
                RecipeMacroSummaryDto.FromDomain(total),
                goalDto,
                goal is null ? null : MacroRemainingDto.FromGoalAndTotal(goal.Target, total)));
        }

        return Result.Ok(new MealPlanRangeDto(query.From, query.To, days));
    }
}
