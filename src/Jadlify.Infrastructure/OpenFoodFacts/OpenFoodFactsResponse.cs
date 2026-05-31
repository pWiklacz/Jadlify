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
    [property: JsonPropertyName("product_quantity")] decimal? ProductQuantity,
    [property: JsonPropertyName("nutriments")] OpenFoodFactsNutriments? Nutriments);

/// <summary>
/// Per-100g nutriment values. OFF keys use hyphens (e.g. <c>energy-kcal_100g</c>), which
/// is why each property carries an explicit <see cref="JsonPropertyNameAttribute"/> that differs
/// from its C# identifier. Always read the <c>*_100g</c> keys for deterministic per-100g
/// storage (off-api-reference §7); the un-suffixed base keys are ambiguous and skipped.
/// Vitamins and minerals are normalized by OFF to grams, so their values are typically
/// sub-milligram (e.g. <c>0.0006</c> g).
/// </summary>
internal sealed record OpenFoodFactsNutriments(
    [property: JsonPropertyName("energy-kcal_100g")] decimal? EnergyKcalPer100g,
    [property: JsonPropertyName("energy-kj_100g")] decimal? EnergyKjPer100g,
    [property: JsonPropertyName("proteins_100g")] decimal? ProteinsPer100g,
    [property: JsonPropertyName("fat_100g")] decimal? FatPer100g,
    [property: JsonPropertyName("carbohydrates_100g")] decimal? CarbohydratesPer100g,
    [property: JsonPropertyName("saturated-fat_100g")] decimal? SaturatedFatPer100g,
    [property: JsonPropertyName("monounsaturated-fat_100g")] decimal? MonounsaturatedFatPer100g,
    [property: JsonPropertyName("polyunsaturated-fat_100g")] decimal? PolyunsaturatedFatPer100g,
    [property: JsonPropertyName("trans-fat_100g")] decimal? TransFatPer100g,
    [property: JsonPropertyName("sugars_100g")] decimal? SugarsPer100g,
    [property: JsonPropertyName("fiber_100g")] decimal? FiberPer100g,
    [property: JsonPropertyName("salt_100g")] decimal? SaltPer100g,
    [property: JsonPropertyName("sodium_100g")] decimal? SodiumPer100g,
    [property: JsonPropertyName("potassium_100g")] decimal? PotassiumPer100g,
    [property: JsonPropertyName("calcium_100g")] decimal? CalciumPer100g,
    [property: JsonPropertyName("iron_100g")] decimal? IronPer100g,
    [property: JsonPropertyName("vitamin-a_100g")] decimal? VitaminAPer100g,
    [property: JsonPropertyName("vitamin-c_100g")] decimal? VitaminCPer100g,
    [property: JsonPropertyName("vitamin-d_100g")] decimal? VitaminDPer100g);
