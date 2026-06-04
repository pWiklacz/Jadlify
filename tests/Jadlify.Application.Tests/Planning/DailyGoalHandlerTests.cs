using Jadlify.Application.Planning;
using Jadlify.Application.Planning.DailyGoals.GetDailyGoal;
using Jadlify.Application.Planning.DailyGoals.UpsertDailyGoal;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Planning;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Planning;

public class DailyGoalHandlerTests
{
    [Fact]
    public async Task GetDailyGoal_ReturnsNullValue_WhenNoGoalConfigured()
    {
        var handler = new GetDailyGoalQueryHandler(new FakeDailyMacroGoalRepository());

        Result<PlanningMacroGoalDto?> result = await handler.HandleAsync(new GetDailyGoalQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetDailyGoal_ReturnsCurrentGoal_WhenConfigured()
    {
        var goal = new DailyMacroGoal(Guid.NewGuid(), new MacroNutrients(2000m, 150m, 60m, 200m));
        var handler = new GetDailyGoalQueryHandler(new FakeDailyMacroGoalRepository(goal));

        Result<PlanningMacroGoalDto?> result = await handler.HandleAsync(new GetDailyGoalQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        PlanningMacroGoalDto dto = Assert.IsType<PlanningMacroGoalDto>(result.Value);
        Assert.Equal(2000m, dto.Calories);
        Assert.Equal(150m, dto.Protein);
        Assert.Equal(60m, dto.Fat);
        Assert.Equal(200m, dto.Carbohydrates);
    }

    [Fact]
    public async Task UpsertDailyGoal_StoresGoalThroughRepository()
    {
        var repository = new FakeDailyMacroGoalRepository();
        var handler = new UpsertDailyGoalCommandHandler(repository);

        Result result = await handler.HandleAsync(
            new UpsertDailyGoalCommand(2200m, 160m, 70m, 210m),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.UpsertCount);
        DailyMacroGoal stored = Assert.IsType<DailyMacroGoal>(repository.Current);
        Assert.Equal(2200m, stored.Target.Calories);
        Assert.Equal(160m, stored.Target.Protein);
    }

    [Fact]
    public async Task UpsertDailyGoal_ReplacesExistingGoal()
    {
        var existing = new DailyMacroGoal(Guid.NewGuid(), new MacroNutrients(1800m, 120m, 50m, 180m));
        var repository = new FakeDailyMacroGoalRepository(existing);
        var handler = new UpsertDailyGoalCommandHandler(repository);

        Result result = await handler.HandleAsync(
            new UpsertDailyGoalCommand(2500m, 180m, 80m, 250m),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        DailyMacroGoal stored = Assert.IsType<DailyMacroGoal>(repository.Current);
        Assert.Equal(2500m, stored.Target.Calories);
    }
}
