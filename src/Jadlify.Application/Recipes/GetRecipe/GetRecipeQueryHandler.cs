using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Planning;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Recipes.GetRecipe;

public sealed class GetRecipeQueryHandler : IQueryHandler<GetRecipeQuery, RecipeDto>
{
    private static readonly Error NotFound =
        Error.NotFound("Recipe.NotFound", "The recipe was not found for the current user.");

    private readonly IRecipeRepository _recipes;
    private readonly IMealPlanRepository _mealPlan;

    public GetRecipeQueryHandler(IRecipeRepository recipes, IMealPlanRepository mealPlan)
    {
        _recipes = recipes;
        _mealPlan = mealPlan;
    }

    public async Task<Result<RecipeDto>> HandleAsync(GetRecipeQuery query, CancellationToken cancellationToken)
    {
        Recipe? recipe = await _recipes.GetByIdAsync(query.Id, cancellationToken);
        if (recipe is null)
        {
            return Result.Fail<RecipeDto>(NotFound);
        }

        IReadOnlyCollection<Guid> used =
            await _mealPlan.ListUsedRecipeIdsAsync([recipe.Id], cancellationToken);

        return Result.Ok(RecipeDto.FromDomain(recipe, used.Count > 0));
    }
}
