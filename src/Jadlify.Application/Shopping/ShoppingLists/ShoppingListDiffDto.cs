using Jadlify.Domain.Shopping;

namespace Jadlify.Application.Shopping.ShoppingLists;

/// <summary>
/// The preview of a refresh. <c>ListVersion</c> and <c>SourceFingerprint</c> are echoed back on
/// confirmation: the version guards against the list having moved on, and the fingerprint
/// against the plan changing again between preview and apply. A confirmation carrying stale
/// values is rejected with a 409 so the user re-previews rather than applying an unseen diff.
/// </summary>
public sealed record ShoppingListDiffDto(
    int ListVersion,
    string SourceFingerprint,
    bool HasChanges,
    IReadOnlyList<ShoppingListDiffLineDto> Added,
    IReadOnlyList<ShoppingListDiffLineDto> Removed,
    IReadOnlyList<ShoppingListDiffLineDto> Changed,
    IReadOnlyList<ShoppingListDiffLineDto> SourceOnly)
{
    public static ShoppingListDiffDto FromDomain(ShoppingListDiff diff, int listVersion, string sourceFingerprint)
    {
        ArgumentNullException.ThrowIfNull(diff);

        return new ShoppingListDiffDto(
            listVersion,
            sourceFingerprint,
            diff.HasChanges,
            [.. diff.Added.Select(ShoppingListDiffLineDto.FromDomain)],
            [.. diff.Removed.Select(ShoppingListDiffLineDto.FromDomain)],
            [.. diff.Changed.Select(ShoppingListDiffLineDto.FromDomain)],
            [.. diff.SourceOnly.Select(ShoppingListDiffLineDto.FromDomain)]);
    }
}

/// <summary>One diff line. <c>PreviousGrams</c> is null for an add, <c>NewGrams</c> null for a remove.</summary>
public sealed record ShoppingListDiffLineDto(
    Guid ProductId,
    string ProductName,
    string? Category,
    decimal? PreviousGrams,
    decimal? NewGrams)
{
    public static ShoppingListDiffLineDto FromDomain(ShoppingListDiffLine line)
    {
        ArgumentNullException.ThrowIfNull(line);

        return new ShoppingListDiffLineDto(
            line.ProductId,
            line.ProductName,
            line.Category?.ToString(),
            line.PreviousGrams,
            line.NewGrams);
    }
}
