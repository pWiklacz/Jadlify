using Jadlify.Application.Common.Mediator;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Planning.MealPlans.DeleteMealPlanEntry;

public sealed class DeleteMealPlanEntryCommandHandler : ICommandHandler<DeleteMealPlanEntryCommand>
{
    private readonly IMealPlanRepository _mealPlans;

    public DeleteMealPlanEntryCommandHandler(IMealPlanRepository mealPlans)
    {
        _mealPlans = mealPlans;
    }

    // The repository is owner-scoped and idempotent: deleting a missing or cross-user
    // entry is a no-op, so deletion always reports success for the current user.
    public async Task<Result> HandleAsync(DeleteMealPlanEntryCommand command, CancellationToken cancellationToken)
    {
        await _mealPlans.DeleteAsync(command.Id, cancellationToken);

        return Result.Ok();
    }
}
