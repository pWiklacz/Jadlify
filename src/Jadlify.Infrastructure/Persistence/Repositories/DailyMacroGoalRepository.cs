using Jadlify.Application.Identity;
using Jadlify.Application.Planning;
using Jadlify.Domain.Planning;
using Microsoft.EntityFrameworkCore;

namespace Jadlify.Infrastructure.Persistence.Repositories;

internal sealed class DailyMacroGoalRepository : IDailyMacroGoalRepository
{
    private readonly JadlifyDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DailyMacroGoalRepository(JadlifyDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<DailyMacroGoal?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        string owner = _currentUser.UserId.Value;

        return await _context.DailyMacroGoals.SingleOrDefaultAsync(
            goal => EF.Property<string>(goal, PersistenceConstants.UserIdProperty) == owner,
            cancellationToken);
    }

    public async Task UpsertAsync(DailyMacroGoal goal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(goal);

        string owner = _currentUser.UserId.Value;

        DailyMacroGoal? existing = await _context.DailyMacroGoals.SingleOrDefaultAsync(
            candidate => EF.Property<string>(candidate, PersistenceConstants.UserIdProperty) == owner,
            cancellationToken);
        if (existing is null)
        {
            _context.DailyMacroGoals.Add(goal);
            _context.Entry(goal).Property(PersistenceConstants.UserIdProperty).CurrentValue = owner;
        }
        else
        {
            _context.Entry(existing).Reference(g => g.Target).TargetEntry!
                .CurrentValues.SetValues(goal.Target);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
