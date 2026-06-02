using Jadlify.API.Common;
using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Recipes;
using Jadlify.Application.Recipes.CreateRecipe;
using Jadlify.Application.Recipes.DeleteRecipe;
using Jadlify.Application.Recipes.GetRecipe;
using Jadlify.Application.Recipes.ListRecipes;
using Jadlify.Application.Recipes.UpdateRecipe;
using Jadlify.SharedKernel;

namespace Jadlify.API.Recipes;

/// <summary>
/// Maps owner-scoped recipe CRUD onto <c>/api/recipes</c>. Routes rely on the
/// global authenticated fallback policy and never opt out with AllowAnonymous.
/// </summary>
public static class RecipeEndpoints
{
    public static IEndpointRouteBuilder MapRecipeEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder recipes = app.MapGroup("/api/recipes");

        recipes.MapGet("/", async (IMediator mediator, CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<RecipeDto>> result =
                await mediator.QueryAsync(new ListRecipesQuery(), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value.Select(RecipeResponse.FromDto).ToArray())
                : result.ToProblem();
        });

        recipes.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<RecipeDto> result =
                await mediator.QueryAsync(new GetRecipeQuery(id), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(RecipeResponse.FromDto(result.Value))
                : result.ToProblem();
        });

        recipes.MapPost("/", async (
            CreateRecipeRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<Guid> result = await mediator.SendAsync(
                new CreateRecipeCommand(
                    request.Name,
                    request.Portions,
                    ToInputs(request.Ingredients)),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/recipes/{result.Value}", new CreatedRecipeResponse(result.Value))
                : result.ToProblem();
        });

        recipes.MapPut("/{id:guid}", async (
            Guid id,
            UpdateRecipeRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result result = await mediator.SendAsync(
                new UpdateRecipeCommand(
                    id,
                    request.Name,
                    request.Portions,
                    ToInputs(request.Ingredients)),
                cancellationToken);

            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        });

        recipes.MapDelete("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result result = await mediator.SendAsync(new DeleteRecipeCommand(id), cancellationToken);

            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        });

        return app;
    }

    private static IReadOnlyList<RecipeIngredientInput> ToInputs(
        IReadOnlyList<RecipeIngredientRequest>? ingredients) =>
        (ingredients ?? []).Select(ingredient =>
            new RecipeIngredientInput(ingredient.ProductId, ingredient.WholeRecipeGrams)).ToArray();
}
