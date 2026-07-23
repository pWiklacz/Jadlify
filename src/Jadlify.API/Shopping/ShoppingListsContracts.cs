using Jadlify.Application.Shopping.ShoppingLists;

namespace Jadlify.API.Shopping;

/// <summary>Creates a list from an arbitrary set of days. Dates are ISO <c>yyyy-MM-dd</c> strings.</summary>
public sealed record CreateShoppingListRequest(string Name, IReadOnlyList<DateOnly> Days);

/// <summary>
/// Ticks one item. <c>ExpectedVersion</c> is the list version the client last saw; a mismatch
/// (the list was refreshed or completed since) is a 409.
/// </summary>
public sealed record ToggleShoppingListItemRequest(bool IsBought, int ExpectedVersion);

/// <summary>
/// Applies a previewed refresh. Both values come from the diff response: <c>ExpectedVersion</c>
/// is the list version and <c>ExpectedSourceFingerprint</c> the projection the preview showed.
/// </summary>
public sealed record RefreshShoppingListRequest(int ExpectedVersion, string ExpectedSourceFingerprint);

/// <summary>Completes the active list. <c>ExpectedVersion</c> guards against a concurrent change.</summary>
public sealed record CompleteShoppingListRequest(int ExpectedVersion);

/// <summary>The index: the single active list (or null) and the completed history.</summary>
public sealed record ShoppingListIndexResponse(
    ShoppingListSummaryResponse? Active,
    IReadOnlyList<ShoppingListSummaryResponse> History)
{
    public static ShoppingListIndexResponse FromDto(ShoppingListIndexDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new ShoppingListIndexResponse(
            dto.Active is null ? null : ShoppingListSummaryResponse.FromDto(dto.Active),
            [.. dto.History.Select(ShoppingListSummaryResponse.FromDto)]);
    }
}

public sealed record ShoppingListSummaryResponse(
    Guid Id,
    string Name,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    int ItemCount,
    int BoughtCount,
    IReadOnlyList<DateOnly> SourceDays)
{
    public static ShoppingListSummaryResponse FromDto(ShoppingListSummaryDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new ShoppingListSummaryResponse(
            dto.Id,
            dto.Name,
            dto.Status,
            dto.CreatedAt,
            dto.CompletedAt,
            dto.ItemCount,
            dto.BoughtCount,
            dto.SourceDays);
    }
}

public sealed record ShoppingListDetailResponse(
    Guid Id,
    string Name,
    string Status,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<DateOnly> SourceDays,
    IReadOnlyList<ShoppingListItemDetailResponse> Items)
{
    public static ShoppingListDetailResponse FromDto(ShoppingListDetailDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new ShoppingListDetailResponse(
            dto.Id,
            dto.Name,
            dto.Status,
            dto.Version,
            dto.CreatedAt,
            dto.CompletedAt,
            dto.SourceDays,
            [.. dto.Items.Select(ShoppingListItemDetailResponse.FromDto)]);
    }
}

public sealed record ShoppingListItemDetailResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? Category,
    decimal Grams,
    bool IsBought,
    IReadOnlyList<ShoppingListItemSourceResponse> Sources)
{
    public static ShoppingListItemDetailResponse FromDto(ShoppingListItemDetailDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new ShoppingListItemDetailResponse(
            dto.Id,
            dto.ProductId,
            dto.ProductName,
            dto.Category,
            dto.Grams,
            dto.IsBought,
            [.. dto.Sources.Select(ShoppingListItemSourceResponse.FromDto)]);
    }
}

public sealed record ShoppingListItemSourceResponse(
    DateOnly Date,
    string MealType,
    string SourceLabel,
    decimal Grams)
{
    public static ShoppingListItemSourceResponse FromDto(ShoppingListItemSourceDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new ShoppingListItemSourceResponse(dto.Date, dto.MealType, dto.SourceLabel, dto.Grams);
    }
}

public sealed record ShoppingListDiffResponse(
    int ListVersion,
    string SourceFingerprint,
    bool HasChanges,
    IReadOnlyList<ShoppingListDiffLineResponse> Added,
    IReadOnlyList<ShoppingListDiffLineResponse> Removed,
    IReadOnlyList<ShoppingListDiffLineResponse> Changed,
    IReadOnlyList<ShoppingListDiffLineResponse> SourceOnly)
{
    public static ShoppingListDiffResponse FromDto(ShoppingListDiffDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new ShoppingListDiffResponse(
            dto.ListVersion,
            dto.SourceFingerprint,
            dto.HasChanges,
            [.. dto.Added.Select(ShoppingListDiffLineResponse.FromDto)],
            [.. dto.Removed.Select(ShoppingListDiffLineResponse.FromDto)],
            [.. dto.Changed.Select(ShoppingListDiffLineResponse.FromDto)],
            [.. dto.SourceOnly.Select(ShoppingListDiffLineResponse.FromDto)]);
    }
}

public sealed record ShoppingListDiffLineResponse(
    Guid ProductId,
    string ProductName,
    string? Category,
    decimal? PreviousGrams,
    decimal? NewGrams)
{
    public static ShoppingListDiffLineResponse FromDto(ShoppingListDiffLineDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new ShoppingListDiffLineResponse(
            dto.ProductId,
            dto.ProductName,
            dto.Category,
            dto.PreviousGrams,
            dto.NewGrams);
    }
}
