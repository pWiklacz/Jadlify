using Jadlify.API.Common;
using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Planning;
using Jadlify.Application.Planning.DailyGoals.GetDailyGoal;
using Jadlify.Application.Planning.DailyGoals.UpsertDailyGoal;
using Jadlify.SharedKernel;

namespace Jadlify.API.Planning;

/// <summary>
/// Maps the single current daily macro goal onto <c>/api/daily-goal</c> as a singleton
/// resource. Routes inherit the global authenticated fallback policy (authenticated + 'sub'
/// claim) and never opt out with AllowAnonymous. A missing goal is a normal empty state
/// returned as JSON <c>null</c> with 200, not an error.
/// </summary>
public static class DailyGoalEndpoints
{
    public static IEndpointRouteBuilder MapDailyGoalEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder goal = app.MapGroup("/api/daily-goal");

        goal.MapGet("/", async (
            HttpContext httpContext,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<PlanningMacroGoalDto?> result =
                await mediator.QueryAsync(new GetDailyGoalQuery(), cancellationToken);

            if (result.IsFailure)
            {
                await result.ToProblem().ExecuteAsync(httpContext);
                return;
            }

            // A missing goal is a normal empty state surfaced as a literal JSON `null` with 200,
            // not a 404. WriteAsJsonAsync serializes null as `null`, whereas Results.Ok(null)
            // would emit an empty body that JSON clients cannot parse.
            DailyGoalResponse? response =
                result.Value is null ? null : DailyGoalResponse.FromDto(result.Value);
            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        });

        goal.MapPut("/", async (
            UpsertDailyGoalRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result result = await mediator.SendAsync(
                new UpsertDailyGoalCommand(
                    request.Calories,
                    request.Protein,
                    request.Fat,
                    request.Carbohydrates),
                cancellationToken);

            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        });

        return app;
    }
}
