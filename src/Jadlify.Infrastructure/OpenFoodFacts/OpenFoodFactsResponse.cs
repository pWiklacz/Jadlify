using System.Text.Json.Serialization;

namespace Jadlify.Infrastructure.OpenFoodFacts;

/// <summary>
/// Deserialization shapes for the OFF get-by-barcode envelope. Only the handful of
/// fields Jadlify snapshots are mapped; everything else in the (large) product object is
/// ignored. <c>status == 1</c> means found; <c>0</c> means not found (off-api-reference §5).
/// </summary>
internal sealed record OpenFoodFactsEnvelope(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("product")] OpenFoodFactsProduct? Product);

internal sealed record OpenFoodFactsProduct(
    [property: JsonPropertyName("product_name")] string? ProductName,
    [property: JsonPropertyName("product_name_pl")] string? ProductNamePl,
    [property: JsonPropertyName("brands")] string? Brands,
    [property: JsonPropertyName("quantity")] string? Quantity,
    [property: JsonPropertyName("nutriments")] OpenFoodFactsNutriments? Nutriments);

/// <summary>
/// Per-100g nutriment values. OFF keys use hyphens (e.g. <c>energy-kcal_100g</c>), which
/// is why each property carries an explicit <see cref="JsonPropertyNameAttribute"/> that differs
/// from its C# identifier. Always read the <c>*_100g</c> keys for deterministic per-100g
/// storage (off-api-reference §7); the un-suffixed base keys are ambiguous and skipped.
/// </summary>
internal sealed record OpenFoodFactsNutriments(
    [property: JsonPropertyName("energy-kcal_100g")] decimal? EnergyKcalPer100g,
    [property: JsonPropertyName("energy-kj_100g")] decimal? EnergyKjPer100g,
    [property: JsonPropertyName("proteins_100g")] decimal? ProteinsPer100g,
    [property: JsonPropertyName("fat_100g")] decimal? FatPer100g,
    [property: JsonPropertyName("carbohydrates_100g")] decimal? CarbohydratesPer100g);
