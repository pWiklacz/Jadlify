using Jadlify.API.Common;
using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Planning;
using Jadlify.Application.Planning.MealPlans.AddMealPlanEntry;
using Jadlify.Application.Planning.MealPlans.DeleteMealPlanEntry;
using Jadlify.Application.Planning.MealPlans.ListMealPlanEntries;
using Jadlify.Application.Planning.MealPlans.UpdateMealPlanEntry;
using Jadlify.Domain.Planning;
using Jadlify.SharedKernel;

namespace Jadlify.API.Planning;

/// <summary>
/// Maps one-day meal-plan CRUD onto <c>/api/meal-plan</c> through the same Minimal API +
/// mediator + <c>ResultExtensions.ToProblem</c> pattern as products and recipes. Routes
/// inherit the global authenticated fallback policy and never opt out with AllowAnonymous.
/// Requests carry meal type names; the boundary parses them to <see cref="MealType"/> and
/// surfaces an invalid name as a 400 field error before the command runs.
/// </summary>
public static class MealPlanEndpoints
{
    public static IEndpointRouteBuilder MapMealPlanEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder mealPlan = app.MapGroup("/api/meal-plan");

        mealPlan.MapGet("/", async (
            DateOnly date,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<MealPlanEntryDto>> result =
                await mediator.QueryAsync(new ListMealPlanEntriesQuery(date), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value.Select(MealPlanEntryResponse.FromDto).ToArray())
                : result.ToProblem();
        });

        mealPlan.MapPost("/", async (
            AddMealPlanEntryRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<MealType> mealType = ParseMealType(request.MealType);
            if (mealType.IsFailure)
            {
                return mealType.ToProblem();
            }

            Result<Guid> result = await mediator.SendAsync(
                new AddMealPlanEntryCommand(request.Date, request.RecipeId, mealType.Value, request.Portions),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created(
                    $"/api/meal-plan/{result.Value}",
                    new CreatedMealPlanEntryResponse(result.Value))
                : result.ToProblem();
        });

        mealPlan.MapPut("/{id:guid}", async (
            Guid id,
            UpdateMealPlanEntryRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<MealType> mealType = ParseMealType(request.MealType);
            if (mealType.IsFailure)
            {
                return mealType.ToProblem();
            }

            Result result = await mediator.SendAsync(
                new UpdateMealPlanEntryCommand(id, mealType.Value, request.Portions),
                cancellationToken);

            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        });

        mealPlan.MapDelete("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result result = await mediator.SendAsync(new DeleteMealPlanEntryCommand(id), cancellationToken);

            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        });

        return app;
    }

    // Parse the wire meal type name into the domain enum. A numeric or unknown value is a
    // client error surfaced as a 400 field problem rather than a defaulted-to-Breakfast entry.
    private static Result<MealType> ParseMealType(string value)
    {
        if (Enum.TryParse(value, ignoreCase: false, out MealType mealType) && Enum.IsDefined(mealType))
        {
            return Result.Ok(mealType);
        }

        return Result.Fail<MealType>(new ValidationError(
        [
            new Error(
                "MealType",
                "The meal type must be one of Breakfast, Lunch, Dinner, or Snack.",
                ErrorType.Validation)
        ]));
    }
}
