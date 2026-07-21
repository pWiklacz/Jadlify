using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Planning;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Recipes.GetRecipeCatalog;

/// <summary>
/// Builds one catalog page with exactly two owner-scoped reads: the recipe page itself and
/// one batched meal-plan usage read for the ids on that page. Usage is never resolved per
/// recipe, so the index cost stays flat as the catalog grows.
/// </summary>
public sealed class GetRecipeCatalogQueryHandler
    : IQueryHandler<GetRecipeCatalogQuery, RecipeCatalogPageDto>
{
    private readonly IRecipeRepository _recipes;
    private readonly IMealPlanRepository _mealPlan;

    public GetRecipeCatalogQueryHandler(IRecipeRepository recipes, IMealPlanRepository mealPlan)
    {
        _recipes = recipes;
        _mealPlan = mealPlan;
    }

    public async Task<Result<RecipeCatalogPageDto>> HandleAsync(
        GetRecipeCatalogQuery query,
        CancellationToken cancellationToken)
    {
        int skip = Math.Max(0, query.Skip ?? 0);
        int requestedTake = query.Take ?? RecipeListBounds.DefaultTake;
        int take = Math.Clamp(requestedTake, 1, RecipeListBounds.MaxTake);

        RecipeCatalogResult page = await _recipes.GetCatalogAsync(
            query.Search,
            query.Sort,
            skip,
            take,
            cancellationToken);

        Guid[] pageIds = [.. page.Items.Select(recipe => recipe.Id)];
        IReadOnlyCollection<Guid> usedIds =
            await _mealPlan.ListUsedRecipeIdsAsync(pageIds, cancellationToken);
        var used = usedIds.ToHashSet();

        IReadOnlyList<RecipeSummaryDto> items =
        [
            .. page.Items.Select(recipe => RecipeSummaryDto.FromDomain(recipe, used.Contains(recipe.Id)))
        ];

        return Result.Ok(new RecipeCatalogPageDto(items, page.Total, skip, take));
    }
}
