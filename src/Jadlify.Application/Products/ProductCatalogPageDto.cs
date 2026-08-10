namespace Jadlify.Application.Products;

/// <summary>
/// One page of the product catalog: the matching <see cref="ProductDto"/> items plus the
/// <see cref="Total"/> count of all matches (ignoring paging) and the effective
/// <see cref="Skip"/>/<see cref="Take"/> window used to produce them. The total lets the UI
/// render "N produktów" and page controls without a second round-trip.
/// </summary>
public sealed record ProductCatalogPageDto(
    IReadOnlyList<ProductDto> Items,
    int Total,
    int Skip,
    int Take);
