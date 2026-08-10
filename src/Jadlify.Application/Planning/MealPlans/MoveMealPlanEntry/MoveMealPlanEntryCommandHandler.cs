using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Planning;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Planning.MealPlans.MoveMealPlanEntry;

public sealed class MoveMealPlanEntryCommandHandler : ICommandHandler<MoveMealPlanEntryCommand>
{
    private static readonly Error NotFound =
        Error.NotFound("MealPlanEntry.NotFound", "The meal-plan entry was not found for the current user.");

    private readonly IMealPlanRepository _mealPlans;

    public MoveMealPlanEntryCommandHandler(IMealPlanRepository mealPlans)
    {
        _mealPlans = mealPlans;
    }

    public async Task<Result> HandleAsync(MoveMealPlanEntryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Owner-scoped load: another user's entry resolves to null and is reported as not found
        // rather than as a forbidden move, so the endpoint never confirms that the id exists.
        MealPlanEntry? entry = await _mealPlans.GetByIdAsync(command.Id, cancellationToken);
        if (entry is null)
        {
            return Result.Fail(NotFound);
        }

        entry.MoveTo(command.Date, command.MealType);

        await _mealPlans.UpdateAsync(entry, cancellationToken);

        return Result.Ok();
    }
}
