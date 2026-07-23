using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Planning;
using Jadlify.Application.Recipes;
using Jadlify.Domain.Shopping;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Shopping.ShoppingLists.RefreshShoppingList;

public sealed class RefreshShoppingListCommandHandler
    : ICommandHandler<RefreshShoppingListCommand, ShoppingListDetailDto>
{
    private readonly IShoppingListRepository _shoppingLists;
    private readonly IMealPlanRepository _mealPlans;
    private readonly IRecipeRepository _recipes;

    public RefreshShoppingListCommandHandler(
        IShoppingListRepository shoppingLists,
        IMealPlanRepository mealPlans,
        IRecipeRepository recipes)
    {
        _shoppingLists = shoppingLists;
        _mealPlans = mealPlans;
        _recipes = recipes;
    }

    public async Task<Result<ShoppingListDetailDto>> HandleAsync(
        RefreshShoppingListCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        ShoppingList? list = await _shoppingLists.GetByIdAsync(command.Id, cancellationToken);
        if (list is null)
        {
            return Result.Fail<ShoppingListDetailDto>(ShoppingListErrors.NotFound);
        }

        if (list.Status is not ShoppingListStatus.Active)
        {
            return Result.Fail<ShoppingListDetailDto>(ShoppingListErrors.NotActive);
        }

        // The list must not have moved on since the preview: a concurrent refresh or completion
        // would have bumped the version, so an apply carrying the old version is stale.
        if (list.Version != command.ExpectedVersion)
        {
            return Result.Fail<ShoppingListDetailDto>(ShoppingListErrors.VersionConflict);
        }

        ShoppingSourceProjection projection = await ShoppingListSourceProjection.BuildAsync(
            _mealPlans,
            _recipes,
            [.. list.SourceDays.Select(day => day.Date)],
            cancellationToken);

        // The plan must not have changed again between preview and apply: if the freshly
        // recomputed fingerprint differs from the one the preview produced, the user would be
        // confirming a diff they never saw, so send them back to re-preview.
        if (projection.Fingerprint != command.ExpectedSourceFingerprint)
        {
            return Result.Fail<ShoppingListDetailDto>(ShoppingListErrors.SourceChanged);
        }

        list.ApplyRefresh(projection.Items, projection.Fingerprint);

        await _shoppingLists.UpdateAsync(list, cancellationToken);

        return Result.Ok(ShoppingListDetailDto.FromDomain(list));
    }
}
