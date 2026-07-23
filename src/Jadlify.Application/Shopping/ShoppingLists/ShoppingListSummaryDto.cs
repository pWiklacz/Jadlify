using Jadlify.Domain.Shopping;

namespace Jadlify.Application.Shopping.ShoppingLists;

/// <summary>
/// A shopping list as shown on the index: identity, status, timestamps, its selected days, and
/// the shopping progress (<c>BoughtCount</c> of <c>ItemCount</c>) so a card can render "X z Y
/// kupione" without loading the whole list.
/// </summary>
public sealed record ShoppingListSummaryDto(
    Guid Id,
    string Name,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    int ItemCount,
    int BoughtCount,
    IReadOnlyList<DateOnly> SourceDays)
{
    public static ShoppingListSummaryDto FromDomain(ShoppingList list)
    {
        ArgumentNullException.ThrowIfNull(list);

        return new ShoppingListSummaryDto(
            list.Id,
            list.Name,
            list.Status.ToString(),
            list.CreatedAt,
            list.CompletedAt,
            list.Items.Count,
            list.Items.Count(item => item.IsBought),
            [.. list.SourceDays.Select(day => day.Date).OrderBy(date => date)]);
    }
}
