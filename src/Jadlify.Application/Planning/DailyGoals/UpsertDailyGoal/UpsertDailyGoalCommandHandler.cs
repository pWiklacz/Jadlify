using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Planning;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Planning.DailyGoals.UpsertDailyGoal;

public sealed class UpsertDailyGoalCommandHandler : ICommandHandler<UpsertDailyGoalCommand>
{
    private readonly IDailyMacroGoalRepository _goals;

    public UpsertDailyGoalCommandHandler(IDailyMacroGoalRepository goals)
    {
        _goals = goals;
    }

    public async Task<Result> HandleAsync(UpsertDailyGoalCommand command, CancellationToken cancellationToken)
    {
        var target = new MacroNutrients(command.Calories, command.Protein, command.Fat, command.Carbohydrates);

        // The repository owns the singleton semantics: it replaces the existing goal's
        // target when one exists, so the id supplied here is only used for a first insert.
        var goal = new DailyMacroGoal(Guid.NewGuid(), target);

        await _goals.UpsertAsync(goal, cancellationToken);

        return Result.Ok();
    }
}
