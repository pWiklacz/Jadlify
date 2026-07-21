using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Planning;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Planning.MealPlans.UpdateMealPlanEntry;

public sealed class UpdateMealPlanEntryCommandHandler : ICommandHandler<UpdateMealPlanEntryCommand>
{
    private static readonly Error NotFound =
        Error.NotFound("MealPlanEntry.NotFound", "The meal-plan entry was not found for the current user.");

    private static readonly Error PortionsExpected = new ValidationError(
    [
        new Error("Portions", "This entry plans a recipe, so its quantity must be portions.", ErrorType.Validation)
    ]);

    private static readonly Error GramsExpected = new ValidationError(
    [
        new Error("Grams", "This entry plans a product, so its quantity must be grams.", ErrorType.Validation)
    ]);

    private readonly IMealPlanRepository _mealPlans;

    public UpdateMealPlanEntryCommandHandler(IMealPlanRepository mealPlans)
    {
        _mealPlans = mealPlans;
    }

    public async Task<Result> HandleAsync(UpdateMealPlanEntryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Load the owner-scoped entry and mutate it through the domain so date and source are
        // preserved; a missing or cross-user entry resolves to null and is reported safely.
        MealPlanEntry? entry = await _mealPlans.GetByIdAsync(command.Id, cancellationToken);
        if (entry is null)
        {
            return Result.Fail(NotFound);
        }

        // The unit must match the stored source. Reading grams as portions would silently
        // rescale the meal, so a mismatched unit is a client error, never a coercion.
        if (entry.Source is MealPlanEntrySource.Recipe && command.Portions is null)
        {
            return Result.Fail(PortionsExpected);
        }

        if (entry.Source is MealPlanEntrySource.Product && command.Grams is null)
        {
            return Result.Fail(GramsExpected);
        }

        entry.UpdateDetails(command.MealType, command.Portions ?? command.Grams!.Value);

        await _mealPlans.UpdateAsync(entry, cancellationToken);

        return Result.Ok();
    }
}
