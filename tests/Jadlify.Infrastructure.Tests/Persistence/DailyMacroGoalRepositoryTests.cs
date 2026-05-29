using Jadlify.Application.Identity;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Planning;
using Jadlify.Infrastructure.Persistence;
using Jadlify.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Jadlify.Infrastructure.Tests.Persistence;

public class DailyMacroGoalRepositoryTests
{
    private static readonly ApplicationUserId OwnerId = new("user-owner");
    private static readonly ApplicationUserId OtherId = new("user-other");

    [Fact]
    public async Task UpsertAsync_CreatesGoal_WhenNoneExists()
    {
        using SqliteTestDatabase database = new();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            DailyMacroGoalRepository repository = new(context, new TestCurrentUser(OwnerId));
            await repository.UpsertAsync(
                new DailyMacroGoal(Guid.NewGuid(), new MacroNutrients(2000m, 150m, 70m, 200m)));
        }

        DailyMacroGoal? current;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            DailyMacroGoalRepository repository = new(context, new TestCurrentUser(OwnerId));
            current = await repository.GetCurrentAsync();
        }

        Assert.NotNull(current);
        Assert.Equal(2000m, current.Target.Calories);
    }

    [Fact]
    public async Task UpsertAsync_UpdatesExistingGoal_WithoutCreatingSecondRow()
    {
        using SqliteTestDatabase database = new();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            DailyMacroGoalRepository repository = new(context, new TestCurrentUser(OwnerId));
            await repository.UpsertAsync(
                new DailyMacroGoal(Guid.NewGuid(), new MacroNutrients(2000m, 150m, 70m, 200m)));
        }

        await using (JadlifyDbContext context = database.CreateContext())
        {
            DailyMacroGoalRepository repository = new(context, new TestCurrentUser(OwnerId));
            await repository.UpsertAsync(
                new DailyMacroGoal(Guid.NewGuid(), new MacroNutrients(1800m, 140m, 60m, 180m)));
        }

        int count;
        DailyMacroGoal? current;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            count = await context.DailyMacroGoals.CountAsync();
            DailyMacroGoalRepository repository = new(context, new TestCurrentUser(OwnerId));
            current = await repository.GetCurrentAsync();
        }

        Assert.Equal(1, count);
        Assert.NotNull(current);
        Assert.Equal(1800m, current.Target.Calories);
    }

    [Fact]
    public async Task GetCurrentAsync_ReturnsOnlyCurrentUsersGoal()
    {
        using SqliteTestDatabase database = new();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            DailyMacroGoalRepository owner = new(context, new TestCurrentUser(OwnerId));
            await owner.UpsertAsync(
                new DailyMacroGoal(Guid.NewGuid(), new MacroNutrients(2000m, 150m, 70m, 200m)));

            DailyMacroGoalRepository other = new(context, new TestCurrentUser(OtherId));
            await other.UpsertAsync(
                new DailyMacroGoal(Guid.NewGuid(), new MacroNutrients(2500m, 180m, 80m, 250m)));
        }

        DailyMacroGoal? ownerGoal;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            DailyMacroGoalRepository repository = new(context, new TestCurrentUser(OwnerId));
            ownerGoal = await repository.GetCurrentAsync();
        }

        Assert.NotNull(ownerGoal);
        Assert.Equal(2000m, ownerGoal.Target.Calories);
    }
}
