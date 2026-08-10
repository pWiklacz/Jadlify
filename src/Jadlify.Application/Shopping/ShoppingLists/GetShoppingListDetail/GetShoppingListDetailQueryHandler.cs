using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Shopping;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Shopping.ShoppingLists.GetShoppingListDetail;

public sealed class GetShoppingListDetailQueryHandler
    : IQueryHandler<GetShoppingListDetailQuery, ShoppingListDetailDto>
{
    private readonly IShoppingListRepository _shoppingLists;

    public GetShoppingListDetailQueryHandler(IShoppingListRepository shoppingLists)
    {
        _shoppingLists = shoppingLists;
    }

    public async Task<Result<ShoppingListDetailDto>> HandleAsync(
        GetShoppingListDetailQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        ShoppingList? list = await _shoppingLists.GetByIdAsync(query.Id, cancellationToken);
        if (list is null)
        {
            return Result.Fail<ShoppingListDetailDto>(ShoppingListErrors.NotFound);
        }

        return Result.Ok(ShoppingListDetailDto.FromDomain(list));
    }
}
