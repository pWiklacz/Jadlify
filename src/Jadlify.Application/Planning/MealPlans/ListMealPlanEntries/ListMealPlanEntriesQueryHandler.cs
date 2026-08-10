using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Recipes;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Planning.MealPlans.ListMealPlanEntries;

public sealed class ListMealPlanEntriesQueryHandler
    : IQueryHandler<ListMealPlanEntriesQuery, IReadOnlyList<MealPlanEntryDto>>
{
    private readonly IMealPlanRepository _mealPlans;
    private readonly IRecipeRepository _recipes;

    public ListMealPlanEntriesQueryHandler(IMealPlanRepository mealPlans, IRecipeRepository recipes)
    {
        _mealPlans = mealPlans;
        _recipes = recipes;
    }

    public async Task<Result<IReadOnlyList<MealPlanEntryDto>>> HandleAsync(
        ListMealPlanEntriesQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<MealPlanEntry> entries = await _mealPlans.ListByDateAsync(query.Date, cancellationToken);
        if (entries.Count == 0)
        {
            return Result.Ok<IReadOnlyList<MealPlanEntryDto>>([]);
        }

        // Resolve current recipe display data in one owner-scoped batch rather than per entry.
        // Product entries carry their own snapshot and contribute no ids to this read.
        Guid[] recipeIds = MealPlanRecipeResolution.DistinctRecipeIds(entries);
        IReadOnlyList<Recipe> recipes = recipeIds.Length == 0
            ? []
            : await _recipes.ListByIdsAsync(recipeIds, cancellationToken);
        var recipeNamesById = recipes.ToDictionary(recipe => recipe.Id, recipe => recipe.Name);

        IReadOnlyList<MealPlanEntryDto> dtos = entries
            .Select(entry => MealPlanEntryDto.FromDomain(
                entry,
                entry.RecipeId is { } recipeId
                    ? recipeNamesById.GetValueOrDefault(recipeId, string.Empty)
                    : null))
            .ToList();

        return Result.Ok(dtos);
    }
}
