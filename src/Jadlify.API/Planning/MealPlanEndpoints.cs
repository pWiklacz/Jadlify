using Jadlify.API.Common;
using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Planning;
using Jadlify.Application.Planning.DailyMacroSummary;
using Jadlify.Application.Planning.MealPlans.AddMealPlanEntry;
using Jadlify.Application.Planning.MealPlans.CopyMealPlanDay;
using Jadlify.Application.Planning.MealPlans.CopyMealPlanEntry;
using Jadlify.Application.Planning.MealPlans.DeleteMealPlanEntry;
using Jadlify.Application.Planning.MealPlans.GetMealPlanRange;
using Jadlify.Application.Planning.MealPlans.ListMealPlanEntries;
using Jadlify.Application.Planning.MealPlans.MoveMealPlanEntry;
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

        mealPlan.MapGet("/summary", async (
            DateOnly date,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<DailyMacroSummaryDto> result =
                await mediator.QueryAsync(new GetDailyMacroSummaryQuery(date), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(DailyMacroSummaryResponse.FromDto(result.Value))
                : result.ToProblem();
        });

        mealPlan.MapGet("/range", async (
            DateOnly from,
            DateOnly to,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<MealPlanRangeDto> result =
                await mediator.QueryAsync(new GetMealPlanRangeQuery(from, to), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(MealPlanRangeResponse.FromDto(result.Value))
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
                new AddMealPlanEntryCommand(
                    request.Date,
                    mealType.Value,
                    request.RecipeId,
                    request.Portions,
                    request.ProductId,
                    request.Grams),
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
                new UpdateMealPlanEntryCommand(id, mealType.Value, request.Portions, request.Grams),
                cancellationToken);

            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        });

        // Move and copy are POSTs on named sub-resources rather than a PUT that happens to
        // change the date: each is one intent the client can name, and copy creates entries a
        // PUT could not report.
        mealPlan.MapPost("/{id:guid}/move", async (
            Guid id,
            MoveMealPlanEntryRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<MealType> mealType = ParseMealType(request.MealType);
            if (mealType.IsFailure)
            {
                return mealType.ToProblem();
            }

            Result result = await mediator.SendAsync(
                new MoveMealPlanEntryCommand(id, request.Date, mealType.Value),
                cancellationToken);

            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        });

        mealPlan.MapPost("/{id:guid}/copies", async (
            Guid id,
            CopyMealPlanEntryRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<CreatedMealPlanEntryDto>> result = await mediator.SendAsync(
                new CopyMealPlanEntryCommand(id, request.TargetDates ?? []),
                cancellationToken);

            // 200 rather than 201: several entries are created at once, so there is no single
            // Location to point at, and the body identifies all of them.
            return result.IsSuccess
                ? Results.Ok(CopiedMealPlanEntriesResponse.FromDtos(result.Value))
                : result.ToProblem();
        });

        mealPlan.MapPost("/days/{date}/copies", async (
            DateOnly date,
            CopyMealPlanDayRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<MealPlanDayCopyMode> mode = ParseCopyMode(request.Mode);
            if (mode.IsFailure)
            {
                return mode.ToProblem();
            }

            Result<IReadOnlyList<CreatedMealPlanEntryDto>> result = await mediator.SendAsync(
                new CopyMealPlanDayCommand(date, request.TargetDates ?? [], mode.Value),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(CopiedMealPlanEntriesResponse.FromDtos(result.Value))
                : result.ToProblem();
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

    // The two copy modes differ in whether the target day's existing entries survive, so an
    // unrecognised value is a 400 rather than a default that might silently discard a day.
    private static Result<MealPlanDayCopyMode> ParseCopyMode(string value)
    {
        if (Enum.TryParse(value, ignoreCase: false, out MealPlanDayCopyMode mode) && Enum.IsDefined(mode))
        {
            return Result.Ok(mode);
        }

        return Result.Fail<MealPlanDayCopyMode>(new ValidationError(
        [
            new Error(
                "Mode",
                "The copy mode must be either Add or Replace.",
                ErrorType.Validation)
        ]));
    }
}
