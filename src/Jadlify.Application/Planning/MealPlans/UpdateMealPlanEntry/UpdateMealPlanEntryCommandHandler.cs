using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Planning;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Planning.MealPlans.UpdateMealPlanEntry;

public sealed class UpdateMealPlanEntryCommandHandler : ICommandHandler<UpdateMealPlanEntryCommand>
{
    private static readonly Error NotFound =
        Error.NotFound("MealPlanEntry.NotFound", "The meal-plan entry was not found for the current user.");

    private readonly IMealPlanRepository _mealPlans;

    public UpdateMealPlanEntryCommandHandler(IMealPlanRepository mealPlans)
    {
        _mealPlans = mealPlans;
    }

    public async Task<Result> HandleAsync(UpdateMealPlanEntryCommand command, CancellationToken cancellationToken)
    {
        // Load the owner-scoped entry and mutate it through the domain so date and recipe
        // are preserved; a missing or cross-user entry resolves to null and is reported safely.
        MealPlanEntry? entry = await _mealPlans.GetByIdAsync(command.Id, cancellationToken);
        if (entry is null)
        {
            return Result.Fail(NotFound);
        }

        entry.UpdateDetails(command.MealType, command.Portions);

        await _mealPlans.UpdateAsync(entry, cancellationToken);

        return Result.Ok();
    }
}
