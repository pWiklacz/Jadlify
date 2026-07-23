using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Planning;
using Jadlify.Application.Recipes;
using Jadlify.Domain.Shopping;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Shopping.ShoppingLists.CreateShoppingList;

public sealed class CreateShoppingListCommandHandler
    : ICommandHandler<CreateShoppingListCommand, ShoppingListDetailDto>
{
    private readonly IShoppingListRepository _shoppingLists;
    private readonly IMealPlanRepository _mealPlans;
    private readonly IRecipeRepository _recipes;
    private readonly TimeProvider _timeProvider;

    public CreateShoppingListCommandHandler(
        IShoppingListRepository shoppingLists,
        IMealPlanRepository mealPlans,
        IRecipeRepository recipes,
        TimeProvider timeProvider)
    {
        _shoppingLists = shoppingLists;
        _mealPlans = mealPlans;
        _recipes = recipes;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ShoppingListDetailDto>> HandleAsync(
        CreateShoppingListCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // One active list at a time: the check gives a clean conflict, and the partial unique
        // index is the backstop for a race that slips past it.
        ShoppingList? active = await _shoppingLists.GetActiveAsync(cancellationToken);
        if (active is not null)
        {
            return Result.Fail<ShoppingListDetailDto>(ShoppingListErrors.ActiveExists);
        }

        ShoppingSourceProjection projection =
            await ShoppingListSourceProjection.BuildAsync(_mealPlans, _recipes, command.Days, cancellationToken);

        var list = ShoppingList.Create(
            Guid.NewGuid(),
            command.Name,
            command.Days,
            projection.Items,
            projection.Fingerprint,
            _timeProvider.GetUtcNow());

        await _shoppingLists.AddAsync(list, cancellationToken);

        return Result.Ok(ShoppingListDetailDto.FromDomain(list));
    }
}
