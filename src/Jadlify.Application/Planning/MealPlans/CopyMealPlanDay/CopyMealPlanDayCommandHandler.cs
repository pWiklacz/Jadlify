using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Planning;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Planning.MealPlans.CopyMealPlanDay;

public sealed class CopyMealPlanDayCommandHandler
    : ICommandHandler<CopyMealPlanDayCommand, IReadOnlyList<CreatedMealPlanEntryDto>>
{
    private static readonly Error EmptySourceDay = new ValidationError(
    [
        new Error(
            nameof(CopyMealPlanDayCommand.SourceDate),
            "The source day has no entries to copy.",
            ErrorType.Validation)
    ]);

    private static readonly Error TooManyEntries = new ValidationError(
    [
        new Error(
            nameof(CopyMealPlanDayCommand.TargetDates),
            $"This copy would create more than {PlanningValidationBounds.MaxCopyCreatedEntries} entries.",
            ErrorType.Validation)
    ]);

    private readonly IMealPlanRepository _mealPlans;

    public CopyMealPlanDayCommandHandler(IMealPlanRepository mealPlans)
    {
        _mealPlans = mealPlans;
    }

    public async Task<Result<IReadOnlyList<CreatedMealPlanEntryDto>>> HandleAsync(
        CopyMealPlanDayCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // One owner-scoped read of the source day; a cross-user date simply yields nothing.
        IReadOnlyList<MealPlanEntry> source = await _mealPlans.ListByDateAsync(
            command.SourceDate,
            cancellationToken);

        // An empty source is rejected rather than treated as a no-op, because under Replace it
        // would quietly clear every target day — a destructive result nobody asked for.
        if (source.Count == 0)
        {
            return Result.Fail<IReadOnlyList<CreatedMealPlanEntryDto>>(EmptySourceDay);
        }

        // The target count alone does not bound the write: a dense day multiplies out. This is
        // the last check before anything is built, so an over-large batch never reaches the
        // repository.
        if ((long)source.Count * command.TargetDates.Count > PlanningValidationBounds.MaxCopyCreatedEntries)
        {
            return Result.Fail<IReadOnlyList<CreatedMealPlanEntryDto>>(TooManyEntries);
        }

        DateOnly[] targets = [.. command.TargetDates.OrderBy(date => date)];
        MealPlanEntry[] copies =
        [
            .. targets.SelectMany(date => source.Select(entry => entry.CopyTo(Guid.NewGuid(), date)))
        ];

        // Both modes are a single write. Replace clears and refills in one call so the target
        // days are never observably empty and a failure leaves the original entries in place.
        if (command.Mode is MealPlanDayCopyMode.Replace)
        {
            await _mealPlans.ReplaceDaysAsync(targets, copies, cancellationToken);
        }
        else
        {
            await _mealPlans.AddRangeAsync(copies, cancellationToken);
        }

        return Result.Ok<IReadOnlyList<CreatedMealPlanEntryDto>>(
        [
            .. copies.Select(copy => new CreatedMealPlanEntryDto(copy.Id, copy.Date, copy.MealType))
        ]);
    }
}
