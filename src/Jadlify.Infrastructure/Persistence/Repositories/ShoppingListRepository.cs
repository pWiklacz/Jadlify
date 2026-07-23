using Jadlify.Application.Identity;
using Jadlify.Application.Shopping.ShoppingLists;
using Jadlify.Domain.Shopping;
using Microsoft.EntityFrameworkCore;

namespace Jadlify.Infrastructure.Persistence.Repositories;

internal sealed class ShoppingListRepository : IShoppingListRepository
{
    private readonly JadlifyDbContext _context;
    private readonly ICurrentUser _currentUser;

    public ShoppingListRepository(JadlifyDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task AddAsync(ShoppingList list, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(list);

        _context.ShoppingLists.Add(list);
        _context.Entry(list).Property(PersistenceConstants.UserIdProperty).CurrentValue =
            _currentUser.UserId.Value;

        // One SaveChanges wraps the root, its items, their sources, and the selected days in a
        // single transaction — a partial list is never persisted.
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ShoppingList?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        string owner = _currentUser.UserId.Value;

        // Items and their sources are regular child entities, so they are pulled in explicitly;
        // the tracked graph is then ready to mutate. Filtering on the partial unique index keeps
        // this to one row. (Source days are owned and load automatically.)
        return await AggregateQuery()
            .SingleOrDefaultAsync(
                list => list.Status == ShoppingListStatus.Active
                    && EF.Property<string>(list, PersistenceConstants.UserIdProperty) == owner,
                cancellationToken);
    }

    public async Task<ShoppingList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        string owner = _currentUser.UserId.Value;

        return await AggregateQuery()
            .SingleOrDefaultAsync(
                list => list.Id == id
                    && EF.Property<string>(list, PersistenceConstants.UserIdProperty) == owner,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ShoppingList>> ListAsync(CancellationToken cancellationToken = default)
    {
        string owner = _currentUser.UserId.Value;

        List<ShoppingList> lists = await AggregateQuery()
            .Where(list => EF.Property<string>(list, PersistenceConstants.UserIdProperty) == owner)
            .ToListAsync(cancellationToken);

        // Active first, then completed newest-first: the index shows the live list on top and
        // history in reverse-chronological order. Ordering in memory keeps the enum-as-string
        // status column from dictating order alphabetically.
        return
        [
            .. lists
                .OrderBy(list => list.Status is ShoppingListStatus.Active ? 0 : 1)
                .ThenByDescending(list => list.CreatedAt)
        ];
    }

    public async Task UpdateAsync(ShoppingList list, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(list);

        // The aggregate was loaded through this same scoped context and is already tracked, so
        // the mutations to its items, sources, and scalar fields are staged. One SaveChanges
        // commits them together; a toggle, refresh, or completion is never half-written.
        await _context.SaveChangesAsync(cancellationToken);
    }

    // Loads the whole aggregate: items and their sources are regular child entities and must be
    // included explicitly (source days are owned and come along on their own).
    private IQueryable<ShoppingList> AggregateQuery() =>
        _context.ShoppingLists
            .Include(list => list.Items)
            .ThenInclude(item => item.Sources);
}
