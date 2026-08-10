using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Recipes;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Planning.DailyMacroSummary;

public sealed class GetDailyMacroSummaryQueryHandler : IQueryHandler<GetDailyMacroSummaryQuery, DailyMacroSummaryDto>
{
    private readonly IMealPlanRepository _mealPlans;
    private readonly IRecipeRepository _recipes;
    private readonly IDailyMacroGoalRepository _goals;

    public GetDailyMacroSummaryQueryHandler(
        IMealPlanRepository mealPlans,
        IRecipeRepository recipes,
        IDailyMacroGoalRepository goals)
    {
        _mealPlans = mealPlans;
        _recipes = recipes;
        _goals = goals;
    }

    public async Task<Result<DailyMacroSummaryDto>> HandleAsync(
        GetDailyMacroSummaryQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<MealPlanEntry> entries = await _mealPlans.ListByDateAsync(query.Date, cancellationToken);

        Guid[] recipeIds = MealPlanRecipeResolution.DistinctRecipeIds(entries);
        IReadOnlyList<Recipe> recipes = recipeIds.Length == 0
            ? []
            : await _recipes.ListByIdsWithIngredientsAsync(recipeIds, cancellationToken);
        var recipesById = recipes.ToDictionary(recipe => recipe.Id);

        (IReadOnlyList<MealEntryMacroDto> entryMacros, MacroNutrients total) =
            MealPlanDayProjection.Build(entries, recipesById);

        DailyMacroGoal? goal = await _goals.GetCurrentAsync(cancellationToken);
        PlanningMacroGoalDto? goalDto = goal is null ? null : PlanningMacroGoalDto.FromDomain(goal);
        MacroRemainingDto? remaining = goal is null ? null : MacroRemainingDto.FromGoalAndTotal(goal.Target, total);

        var dto = new DailyMacroSummaryDto(
            query.Date,
            entryMacros,
            RecipeMacroSummaryDto.FromDomain(total),
            goalDto,
            remaining);

        return Result.Ok(dto);
    }
}
