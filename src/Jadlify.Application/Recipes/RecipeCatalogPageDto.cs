namespace Jadlify.Application.Recipes;

/// <summary>
/// One page of the recipe catalog: the matching <see cref="RecipeSummaryDto"/> items plus
/// the <see cref="Total"/> count of all matches (ignoring paging) and the effective
/// <see cref="Skip"/>/<see cref="Take"/> window used to produce them. The total lets the UI
/// render "N przepisów" and page controls without a second round-trip.
/// </summary>
public sealed record RecipeCatalogPageDto(
    IReadOnlyList<RecipeSummaryDto> Items,
    int Total,
    int Skip,
    int Take);
