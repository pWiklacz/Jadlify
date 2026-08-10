using Jadlify.Domain.Shopping;

namespace Jadlify.Application.Shopping.ShoppingLists;

/// <summary>
/// Owner-scoped persistence for the shopping-list aggregate. Every read filters by the current
/// user and returns the whole aggregate — items, their source contributions, and the selected
/// days — because those are only ever reached through the root. Writes go through one
/// <c>SaveChanges</c> so a create, refresh, or complete is a single transaction.
/// </summary>
public interface IShoppingListRepository
{
    /// <summary>Adds a new list as the current user's in one write.</summary>
    Task AddAsync(ShoppingList list, CancellationToken cancellationToken = default);

    /// <summary>The current user's single active list, or <c>null</c> if none is active.</summary>
    Task<ShoppingList?> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>The current user's list with <paramref name="id"/> in any status, or <c>null</c>.</summary>
    Task<ShoppingList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Every list the current user owns, for the index (active plus history).</summary>
    Task<IReadOnlyList<ShoppingList>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists changes to a <paramref name="list"/> the current user loaded and mutated. The
    /// aggregate is already tracked, so this is the single <c>SaveChanges</c> that commits a
    /// toggle, refresh, or completion together with its owned-collection changes.
    /// </summary>
    Task UpdateAsync(ShoppingList list, CancellationToken cancellationToken = default);
}
