namespace Jadlify.Application.Products;

/// <summary>
/// Application-owned port for fetching product data by barcode from an external
/// source (implemented in Infrastructure). A miss or an outage returns
/// <c>null</c> — implementations must never throw for a not-found or a failure,
/// so the lookup can never block product creation (FR-006).
/// </summary>
public interface IBarcodeProductLookup
{
    Task<BarcodeProductData?> LookupAsync(string barcode, CancellationToken cancellationToken = default);
}

/// <summary>
/// Pre-fill data mapped from an external source. Every field is optional:
/// crowdsourced sources frequently lack one or more macros, and a missing field
/// simply leaves the corresponding form input blank.
/// </summary>
public sealed record BarcodeProductData(
    string? Name = null,
    string? Brand = null,
    decimal? Calories = null,
    decimal? Protein = null,
    decimal? Fat = null,
    decimal? Carbohydrates = null);
