using Jadlify.API.Common;
using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Shopping.ShoppingLists;
using Jadlify.Application.Shopping.ShoppingLists.CompleteShoppingList;
using Jadlify.Application.Shopping.ShoppingLists.CreateShoppingList;
using Jadlify.Application.Shopping.ShoppingLists.GetShoppingListDetail;
using Jadlify.Application.Shopping.ShoppingLists.GetShoppingListDiff;
using Jadlify.Application.Shopping.ShoppingLists.GetShoppingListIndex;
using Jadlify.Application.Shopping.ShoppingLists.RefreshShoppingList;
using Jadlify.Application.Shopping.ShoppingLists.ToggleShoppingListItem;
using Jadlify.SharedKernel;

namespace Jadlify.API.Shopping;

/// <summary>
/// Maps the persistent shopping-list aggregate onto <c>/api/shopping-lists</c>. Every route
/// inherits the global authenticated fallback policy (never AllowAnonymous) and shapes failures
/// through <c>ResultExtensions.ToProblem</c>, so ownership, concurrency, and validation surface
/// as 404 / 409 / 400 uniformly. This lives beside the old read-only <c>/api/shopping-list</c>
/// projection, which stays until the UI migrates.
/// </summary>
public static class ShoppingListsEndpoints
{
    public static IEndpointRouteBuilder MapShoppingListsEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder lists = app.MapGroup("/api/shopping-lists");

        lists.MapGet("/", async (IMediator mediator, CancellationToken cancellationToken) =>
        {
            Result<ShoppingListIndexDto> result =
                await mediator.QueryAsync(new GetShoppingListIndexQuery(), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(ShoppingListIndexResponse.FromDto(result.Value))
                : result.ToProblem();
        });

        lists.MapPost("/", async (
            CreateShoppingListRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<ShoppingListDetailDto> result = await mediator.SendAsync(
                new CreateShoppingListCommand(request.Name, request.Days ?? []),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created(
                    $"/api/shopping-lists/{result.Value.Id}",
                    ShoppingListDetailResponse.FromDto(result.Value))
                : result.ToProblem();
        });

        lists.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<ShoppingListDetailDto> result =
                await mediator.QueryAsync(new GetShoppingListDetailQuery(id), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(ShoppingListDetailResponse.FromDto(result.Value))
                : result.ToProblem();
        });

        lists.MapPatch("/{id:guid}/items/{itemId:guid}", async (
            Guid id,
            Guid itemId,
            ToggleShoppingListItemRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<ShoppingListDetailDto> result = await mediator.SendAsync(
                new ToggleShoppingListItemCommand(id, itemId, request.IsBought, request.ExpectedVersion),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(ShoppingListDetailResponse.FromDto(result.Value))
                : result.ToProblem();
        });

        lists.MapGet("/{id:guid}/diff", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<ShoppingListDiffDto> result =
                await mediator.QueryAsync(new GetShoppingListDiffQuery(id), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(ShoppingListDiffResponse.FromDto(result.Value))
                : result.ToProblem();
        });

        lists.MapPost("/{id:guid}/refresh", async (
            Guid id,
            RefreshShoppingListRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<ShoppingListDetailDto> result = await mediator.SendAsync(
                new RefreshShoppingListCommand(id, request.ExpectedVersion, request.ExpectedSourceFingerprint),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(ShoppingListDetailResponse.FromDto(result.Value))
                : result.ToProblem();
        });

        lists.MapPost("/{id:guid}/complete", async (
            Guid id,
            CompleteShoppingListRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<ShoppingListDetailDto> result = await mediator.SendAsync(
                new CompleteShoppingListCommand(id, request.ExpectedVersion),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(ShoppingListDetailResponse.FromDto(result.Value))
                : result.ToProblem();
        });

        return app;
    }
}
