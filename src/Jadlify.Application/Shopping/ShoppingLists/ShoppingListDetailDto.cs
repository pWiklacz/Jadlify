using Jadlify.Domain.Shopping;

namespace Jadlify.Application.Shopping.ShoppingLists;

/// <summary>
/// The full state of one shopping list. <c>Version</c> is the optimistic-concurrency token a
/// caller echoes back on a toggle, refresh, or complete. Enum-valued fields are strings so the
/// wire stays stable and the UI maps its own Polish labels.
/// </summary>
public sealed record ShoppingListDetailDto(
    Guid Id,
    string Name,
    string Status,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<DateOnly> SourceDays,
    IReadOnlyList<ShoppingListItemDetailDto> Items)
{
    public static ShoppingListDetailDto FromDomain(ShoppingList list)
    {
        ArgumentNullException.ThrowIfNull(list);

        return new ShoppingListDetailDto(
            list.Id,
            list.Name,
            list.Status.ToString(),
            list.Version,
            list.CreatedAt,
            list.CompletedAt,
            [.. list.SourceDays.Select(day => day.Date).OrderBy(date => date)],
            [.. list.Items
                .Select(ShoppingListItemDetailDto.FromDomain)
                .OrderBy(item => item.ProductName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.ProductId)]);
    }
}

/// <summary>One shopping line: its stable id, product snapshot, grams, bought state, and sources.</summary>
public sealed record ShoppingListItemDetailDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? Category,
    decimal Grams,
    bool IsBought,
    IReadOnlyList<ShoppingListItemSourceDto> Sources)
{
    public static ShoppingListItemDetailDto FromDomain(ShoppingListItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new ShoppingListItemDetailDto(
            item.Id,
            item.ProductId,
            item.ProductName,
            item.Category?.ToString(),
            item.Grams,
            item.IsBought,
            [.. item.Sources.Select(ShoppingListItemSourceDto.FromDomain)]);
    }
}

/// <summary>One meal's contribution to a line, for the "by meal" / "by day" views and "used in".</summary>
public sealed record ShoppingListItemSourceDto(
    DateOnly Date,
    string MealType,
    string SourceLabel,
    decimal Grams)
{
    public static ShoppingListItemSourceDto FromDomain(ShoppingListItemSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new ShoppingListItemSourceDto(
            source.Date,
            source.MealType.ToString(),
            source.SourceLabel,
            source.Grams);
    }
}
