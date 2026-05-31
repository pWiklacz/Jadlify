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

    /// <summary>EAN-8 through GTIN-14, digits only, not normalized client-side.</summary>
    public const string BarcodePattern = @"^\d{8,14}$";
}
