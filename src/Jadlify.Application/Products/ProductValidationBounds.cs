namespace Jadlify.Application.Products;

/// <summary>
/// Shared validation bounds for product commands. Kept in one place so the
/// Create and Update validators apply identical limits. Bounds are
/// "minimal-plus-sane": they reject physically impossible per-100g values
/// without attempting calorie↔macro coherence checks.
/// </summary>
public static class ProductValidationBounds
{
    public const int MaxNameLength = 200;

    /// <summary>A macro (protein/fat/carbs) cannot exceed 100 g per 100 g.</summary>
    public const decimal MaxMacroGramsPer100g = 100m;

    /// <summary>
    /// Practical upper bound for kcal per 100 g — pure fat is ~900 kcal/100g,
    /// which is the densest food macro, so nothing realistic exceeds this.
    /// </summary>
    public const decimal MaxCaloriesPer100g = 900m;

    /// <summary>
    /// Upper bound for any extended per-100g nutrient (fat components, sugars, fiber,
    /// salt, and the gram-stored minerals/vitamins). All are grams per 100 g, so 100 is
    /// the physical ceiling — generous for the sub-gram micros, which never approach it.
    /// </summary>
    public const decimal MaxNutrientGramsPer100g = 100m;

    /// <summary>
    /// Upper bound for net package size in grams (~100 kg) — far above any realistic
    /// retail food package while still rejecting obvious data-entry mistakes.
    /// </summary>
    public const decimal MaxPackageSizeGrams = 100_000m;

    /// <summary>EAN-8 through GTIN-14, digits only, not normalized client-side.</summary>
    public const string BarcodePattern = @"^\d{8,14}$";
}
