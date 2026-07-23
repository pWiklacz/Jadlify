using Jadlify.Application.Shopping.ShoppingLists;
using Jadlify.Domain.Shopping;

namespace Jadlify.Application.Tests.Shopping;

/// <summary>
/// In-memory <see cref="IShoppingListRepository"/> for handler unit tests. Stores aggregates by
/// reference, so a handler that loads a list, mutates it, and calls <see cref="UpdateAsync"/>
/// sees the change reflected exactly as the tracked EF context would.
/// </summary>
internal sealed class FakeShoppingListRepository : IShoppingListRepository
{
    private readonly List<ShoppingList> _lists;

    public FakeShoppingListRepository(params ShoppingList[] seed)
    {
        _lists = [.. seed];
    }

    public IReadOnlyList<ShoppingList> Lists => _lists;

    public int AddCount { get; private set; }

    public int UpdateCount { get; private set; }

    public Task AddAsync(ShoppingList list, CancellationToken cancellationToken = default)
    {
        AddCount++;
        _lists.Add(list);
        return Task.CompletedTask;
    }

    public Task<ShoppingList?> GetActiveAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_lists.SingleOrDefault(list => list.Status is ShoppingListStatus.Active));

    public Task<ShoppingList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_lists.SingleOrDefault(list => list.Id == id));

    public Task<IReadOnlyList<ShoppingList>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ShoppingList>>(
        [
            .. _lists
                .OrderBy(list => list.Status is ShoppingListStatus.Active ? 0 : 1)
                .ThenByDescending(list => list.CreatedAt)
        ]);

    public Task UpdateAsync(ShoppingList list, CancellationToken cancellationToken = default)
    {
        UpdateCount++;
        return Task.CompletedTask;
    }
}
