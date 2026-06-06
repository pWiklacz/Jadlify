using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Planning;
using Jadlify.Application.Recipes;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Recipes;
using Jadlify.Domain.Shopping;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Shopping.GetShoppingList;

public sealed class GetShoppingListQueryHandler : IQueryHandler<GetShoppingListQuery, ShoppingListDto>
{
    private const string MissingRecipeWarningMessage =
        "Recipe was not found for this meal-plan entry, so its ingredients were not included.";

    private readonly IMealPlanRepository _mealPlans;
    private readonly IRecipeRepository _recipes;

    public GetShoppingListQueryHandler(IMealPlanRepository mealPlans, IRecipeRepository recipes)
    {
        _mealPlans = mealPlans;
        _recipes = recipes;
    }

    public async Task<Result<ShoppingListDto>> HandleAsync(
        GetShoppingListQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<MealPlanEntry> entries = await _mealPlans.ListByDateAsync(query.Date, cancellationToken);

        Guid[] recipeIds = entries.Select(entry => entry.RecipeId).Distinct().ToArray();
        IReadOnlyList<Recipe> recipes = await _recipes.ListByIdsWithIngredientsAsync(recipeIds, cancellationToken);
        var recipesById = recipes.ToDictionary(recipe => recipe.Id);

        List<(MealPlanEntry Entry, Recipe Recipe)> matchedEntries = [];
        List<ShoppingListWarningDto> warnings = [];

        foreach (MealPlanEntry entry in entries)
        {
            if (recipesById.TryGetValue(entry.RecipeId, out Recipe? recipe))
            {
                matchedEntries.Add((entry, recipe));
                continue;
            }

            warnings.Add(new ShoppingListWarningDto(
                entry.Id,
                entry.RecipeId,
                MissingRecipeWarningMessage));
        }

        IReadOnlyList<ShoppingListItemDto> items = ShoppingListCalculator.ForMealEntries(matchedEntries)
            .Select(item => new ShoppingListItemDto(item.ProductId, item.ProductName, item.Grams))
            .OrderBy(item => item.ProductName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ProductId)
            .ToList();

        return Result.Ok(new ShoppingListDto(query.Date, items, warnings));
    }
}
