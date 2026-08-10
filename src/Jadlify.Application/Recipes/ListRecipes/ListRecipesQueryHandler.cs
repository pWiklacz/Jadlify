using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Planning;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Recipes.ListRecipes;

public sealed class ListRecipesQueryHandler : IQueryHandler<ListRecipesQuery, IReadOnlyList<RecipeDto>>
{
    private readonly IRecipeRepository _recipes;
    private readonly IMealPlanRepository _mealPlan;

    public ListRecipesQueryHandler(IRecipeRepository recipes, IMealPlanRepository mealPlan)
    {
        _recipes = recipes;
        _mealPlan = mealPlan;
    }

    public async Task<Result<IReadOnlyList<RecipeDto>>> HandleAsync(
        ListRecipesQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Recipe> recipes = await _recipes.ListAsync(cancellationToken);

        // One batched usage read for the whole list, not one per recipe.
        IReadOnlyCollection<Guid> usedIds = await _mealPlan.ListUsedRecipeIdsAsync(
            [.. recipes.Select(recipe => recipe.Id)],
            cancellationToken);
        var used = usedIds.ToHashSet();

        return Result.Ok<IReadOnlyList<RecipeDto>>(
            [.. recipes.Select(recipe => RecipeDto.FromDomain(recipe, used.Contains(recipe.Id)))]);
    }
}
