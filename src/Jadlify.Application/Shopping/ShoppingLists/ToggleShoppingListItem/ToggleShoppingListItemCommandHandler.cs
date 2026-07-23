using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Shopping;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Shopping.ShoppingLists.ToggleShoppingListItem;

public sealed class ToggleShoppingListItemCommandHandler
    : ICommandHandler<ToggleShoppingListItemCommand, ShoppingListDetailDto>
{
    private readonly IShoppingListRepository _shoppingLists;

    public ToggleShoppingListItemCommandHandler(IShoppingListRepository shoppingLists)
    {
        _shoppingLists = shoppingLists;
    }

    public async Task<Result<ShoppingListDetailDto>> HandleAsync(
        ToggleShoppingListItemCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        ShoppingList? list = await _shoppingLists.GetByIdAsync(command.ListId, cancellationToken);
        if (list is null)
        {
            return Result.Fail<ShoppingListDetailDto>(ShoppingListErrors.NotFound);
        }

        if (list.Status is not ShoppingListStatus.Active)
        {
            return Result.Fail<ShoppingListDetailDto>(ShoppingListErrors.NotActive);
        }

        if (list.Version != command.ExpectedVersion)
        {
            return Result.Fail<ShoppingListDetailDto>(ShoppingListErrors.VersionConflict);
        }

        if (!list.TrySetItemBought(command.ItemId, command.IsBought))
        {
            return Result.Fail<ShoppingListDetailDto>(ShoppingListErrors.ItemNotFound);
        }

        await _shoppingLists.UpdateAsync(list, cancellationToken);

        return Result.Ok(ShoppingListDetailDto.FromDomain(list));
    }
}
