using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Planning;
using Jadlify.Application.Recipes;
using Jadlify.Domain.Shopping;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Shopping.ShoppingLists.GetShoppingListDiff;

public sealed class GetShoppingListDiffQueryHandler
    : IQueryHandler<GetShoppingListDiffQuery, ShoppingListDiffDto>
{
    private readonly IShoppingListRepository _shoppingLists;
    private readonly IMealPlanRepository _mealPlans;
    private readonly IRecipeRepository _recipes;

    public GetShoppingListDiffQueryHandler(
        IShoppingListRepository shoppingLists,
        IMealPlanRepository mealPlans,
        IRecipeRepository recipes)
    {
        _shoppingLists = shoppingLists;
        _mealPlans = mealPlans;
        _recipes = recipes;
    }

    public async Task<Result<ShoppingListDiffDto>> HandleAsync(
        GetShoppingListDiffQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        ShoppingList? list = await _shoppingLists.GetByIdAsync(query.Id, cancellationToken);
        if (list is null)
        {
            return Result.Fail<ShoppingListDiffDto>(ShoppingListErrors.NotFound);
        }

        // A completed list is frozen; there is nothing to refresh into it.
        if (list.Status is not ShoppingListStatus.Active)
        {
            return Result.Fail<ShoppingListDiffDto>(ShoppingListErrors.NotActive);
        }

        ShoppingSourceProjection projection = await ShoppingListSourceProjection.BuildAsync(
            _mealPlans,
            _recipes,
            [.. list.SourceDays.Select(day => day.Date)],
            cancellationToken);

        ShoppingListDiff diff = list.PreviewRefresh(projection.Items);

        return Result.Ok(ShoppingListDiffDto.FromDomain(diff, list.Version, projection.Fingerprint));
    }
}
