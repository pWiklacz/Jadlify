using Jadlify.Application.Identity;
using Jadlify.Application.Planning;
using Jadlify.Domain.Planning;
using Microsoft.EntityFrameworkCore;

namespace Jadlify.Infrastructure.Persistence.Repositories;

internal sealed class MealPlanRepository : IMealPlanRepository
{
    private readonly JadlifyDbContext _context;
    private readonly ICurrentUser _currentUser;

    public MealPlanRepository(JadlifyDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task AddAsync(MealPlanEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _context.MealPlanEntries.Add(entry);
        _context.Entry(entry).Property(PersistenceConstants.UserIdProperty).CurrentValue =
            _currentUser.UserId.Value;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<MealPlanEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        string owner = _currentUser.UserId.Value;

        return await _context.MealPlanEntries.SingleOrDefaultAsync(
            entry => entry.Id == id
                && EF.Property<string>(entry, PersistenceConstants.UserIdProperty) == owner,
            cancellationToken);
    }

    public async Task<IReadOnlyList<MealPlanEntry>> ListByDateAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        string owner = _currentUser.UserId.Value;

        return await _context.MealPlanEntries
            .Where(entry => entry.Date == date
                && EF.Property<string>(entry, PersistenceConstants.UserIdProperty) == owner)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(MealPlanEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        string owner = _currentUser.UserId.Value;

        MealPlanEntry? existing = await _context.MealPlanEntries.SingleOrDefaultAsync(
            candidate => candidate.Id == entry.Id
                && EF.Property<string>(candidate, PersistenceConstants.UserIdProperty) == owner,
            cancellationToken);
        if (existing is null)
        {
            return;
        }

        _context.Entry(existing).CurrentValues.SetValues(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        string owner = _currentUser.UserId.Value;

        MealPlanEntry? existing = await _context.MealPlanEntries.SingleOrDefaultAsync(
            entry => entry.Id == id
                && EF.Property<string>(entry, PersistenceConstants.UserIdProperty) == owner,
            cancellationToken);
        if (existing is null)
        {
            return;
        }

        _context.MealPlanEntries.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
