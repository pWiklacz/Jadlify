using Jadlify.API.Common;
using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Shopping;
using Jadlify.Application.Shopping.GetShoppingList;
using Jadlify.SharedKernel;

namespace Jadlify.API.Shopping;

public static class ShoppingListEndpoints
{
    public static IEndpointRouteBuilder MapShoppingListEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder shoppingList = app.MapGroup("/api/shopping-list");

        shoppingList.MapGet("/", async (
            DateOnly date,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<ShoppingListDto> result =
                await mediator.QueryAsync(new GetShoppingListQuery(date), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(ShoppingListResponse.FromDto(result.Value))
                : result.ToProblem();
        });

        return app;
    }
}
