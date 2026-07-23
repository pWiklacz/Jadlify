using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Shopping.ShoppingLists.CompleteShoppingList;

/// <summary>
/// Finishes the active list, freezing it into immutable history. <see cref="ExpectedVersion"/>
/// guards against completing a list that was refreshed since it was loaded.
/// </summary>
public sealed record CompleteShoppingListCommand(
    Guid Id,
    int ExpectedVersion) : ICommand<ShoppingListDetailDto>;
