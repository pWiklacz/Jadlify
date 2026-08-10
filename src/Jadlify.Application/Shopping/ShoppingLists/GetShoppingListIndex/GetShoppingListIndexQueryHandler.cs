using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Shopping;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Shopping.ShoppingLists.GetShoppingListIndex;

public sealed class GetShoppingListIndexQueryHandler
    : IQueryHandler<GetShoppingListIndexQuery, ShoppingListIndexDto>
{
    private readonly IShoppingListRepository _shoppingLists;

    public GetShoppingListIndexQueryHandler(IShoppingListRepository shoppingLists)
    {
        _shoppingLists = shoppingLists;
    }

    public async Task<Result<ShoppingListIndexDto>> HandleAsync(
        GetShoppingListIndexQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ShoppingList> lists = await _shoppingLists.ListAsync(cancellationToken);

        // The repository returns the active list first (there is at most one), then completed
        // lists newest-first, so the split here needs no re-sorting.
        ShoppingListSummaryDto? active = lists
            .Where(list => list.Status is ShoppingListStatus.Active)
            .Select(ShoppingListSummaryDto.FromDomain)
            .FirstOrDefault();

        IReadOnlyList<ShoppingListSummaryDto> history =
        [
            .. lists
                .Where(list => list.Status is ShoppingListStatus.Completed)
                .Select(ShoppingListSummaryDto.FromDomain)
        ];

        return Result.Ok(new ShoppingListIndexDto(active, history));
    }
}
