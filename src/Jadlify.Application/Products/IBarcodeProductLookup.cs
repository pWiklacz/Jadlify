using Jadlify.Domain.Products;

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
/// crowdsourced sources frequently lack one or more values, and a missing field
/// simply leaves the corresponding form input blank. Beyond the core macros it also
/// carries the net package size and the extended per-100g profile (fat breakdown,
/// sugars/fiber, salt/sodium/potassium, and a small micronutrient set). <see cref="Category"/>
/// is a best-effort taxonomy suggestion mapped from the external source; an unrecognized
/// category maps to <c>null</c>, never an error.
/// </summary>
public sealed record BarcodeProductData(
    string? Name = null,
    string? Brand = null,
    decimal? Calories = null,
    decimal? Protein = null,
    decimal? Fat = null,
    decimal? Carbohydrates = null,
    decimal? PackageSizeGrams = null,
    decimal? SaturatedFat = null,
    decimal? MonounsaturatedFat = null,
    decimal? PolyunsaturatedFat = null,
    decimal? TransFat = null,
    decimal? Sugars = null,
    decimal? Fiber = null,
    decimal? Salt = null,
    decimal? Sodium = null,
    decimal? Potassium = null,
    decimal? Calcium = null,
    decimal? Iron = null,
    decimal? VitaminA = null,
    decimal? VitaminC = null,
    decimal? VitaminD = null,
    ProductCategory? Category = null);
