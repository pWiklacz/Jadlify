using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Planning;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Planning.MealPlans.CopyMealPlanEntry;

public sealed class CopyMealPlanEntryCommandHandler
    : ICommandHandler<CopyMealPlanEntryCommand, IReadOnlyList<CreatedMealPlanEntryDto>>
{
    private static readonly Error NotFound =
        Error.NotFound("MealPlanEntry.NotFound", "The meal-plan entry was not found for the current user.");

    private readonly IMealPlanRepository _mealPlans;

    public CopyMealPlanEntryCommandHandler(IMealPlanRepository mealPlans)
    {
        _mealPlans = mealPlans;
    }

    public async Task<Result<IReadOnlyList<CreatedMealPlanEntryDto>>> HandleAsync(
        CopyMealPlanEntryCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Owner-scoped load first: a cross-user source id must fail before any copy exists.
        MealPlanEntry? source = await _mealPlans.GetByIdAsync(command.Id, cancellationToken);
        if (source is null)
        {
            return Result.Fail<IReadOnlyList<CreatedMealPlanEntryDto>>(NotFound);
        }

        // One copy per target day, so the validated target count already bounds the write.
        MealPlanEntry[] copies =
        [
            .. command.TargetDates
                .OrderBy(date => date)
                .Select(date => source.CopyTo(Guid.NewGuid(), date))
        ];

        await _mealPlans.AddRangeAsync(copies, cancellationToken);

        return Result.Ok<IReadOnlyList<CreatedMealPlanEntryDto>>(
        [
            .. copies.Select(copy => new CreatedMealPlanEntryDto(copy.Id, copy.Date, copy.MealType))
        ]);
    }
}
