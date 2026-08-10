using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Shopping;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Shopping.ShoppingLists.CompleteShoppingList;

public sealed class CompleteShoppingListCommandHandler
    : ICommandHandler<CompleteShoppingListCommand, ShoppingListDetailDto>
{
    private readonly IShoppingListRepository _shoppingLists;
    private readonly TimeProvider _timeProvider;

    public CompleteShoppingListCommandHandler(
        IShoppingListRepository shoppingLists,
        TimeProvider timeProvider)
    {
        _shoppingLists = shoppingLists;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ShoppingListDetailDto>> HandleAsync(
        CompleteShoppingListCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        ShoppingList? list = await _shoppingLists.GetByIdAsync(command.Id, cancellationToken);
        if (list is null)
        {
            return Result.Fail<ShoppingListDetailDto>(ShoppingListErrors.NotFound);
        }

        // Completing an already-completed list is a conflict, not a silent success: the caller
        // is acting on a stale view of a frozen list.
        if (list.Status is not ShoppingListStatus.Active)
        {
            return Result.Fail<ShoppingListDetailDto>(ShoppingListErrors.NotActive);
        }

        if (list.Version != command.ExpectedVersion)
        {
            return Result.Fail<ShoppingListDetailDto>(ShoppingListErrors.VersionConflict);
        }

        list.Complete(_timeProvider.GetUtcNow());

        await _shoppingLists.UpdateAsync(list, cancellationToken);

        return Result.Ok(ShoppingListDetailDto.FromDomain(list));
    }
}
