using System.Runtime.CompilerServices;

namespace Jadlify.Domain.Nutrition;

/// <summary>
/// Extended per-100g nutrition profile, sitting alongside the core
/// <see cref="MacroNutrients"/>: the fat breakdown (saturated / mono- / poly- / trans),
/// sugars and fiber, salt / sodium / potassium, and a small micronutrient set
/// (calcium, iron, vitamins A / C / D). Every field is optional — Open Food Facts
/// coverage is sparse and manual entry is always the fallback (FR-006) — so a missing
/// value stays <c>null</c>. Values are grams per 100 g (OFF normalizes vitamins and
/// minerals to grams, hence the sub-milligram magnitudes). Pure value object: it only
/// guards each present value against being negative.
/// </summary>
public sealed record NutritionFacts
{
    public NutritionFacts(
        decimal? saturatedFat = null,
        decimal? monounsaturatedFat = null,
        decimal? polyunsaturatedFat = null,
        decimal? transFat = null,
        decimal? sugars = null,
        decimal? fiber = null,
        decimal? salt = null,
        decimal? sodium = null,
        decimal? potassium = null,
        decimal? calcium = null,
        decimal? iron = null,
        decimal? vitaminA = null,
        decimal? vitaminC = null,
        decimal? vitaminD = null)
    {
        SaturatedFat = NonNegative(saturatedFat);
        MonounsaturatedFat = NonNegative(monounsaturatedFat);
        PolyunsaturatedFat = NonNegative(polyunsaturatedFat);
        TransFat = NonNegative(transFat);
        Sugars = NonNegative(sugars);
        Fiber = NonNegative(fiber);
        Salt = NonNegative(salt);
        Sodium = NonNegative(sodium);
        Potassium = NonNegative(potassium);
        Calcium = NonNegative(calcium);
        Iron = NonNegative(iron);
        VitaminA = NonNegative(vitaminA);
        VitaminC = NonNegative(vitaminC);
        VitaminD = NonNegative(vitaminD);
    }

    /// <summary>
    /// An all-null profile — no extended data recorded for the product yet. Returns a
    /// fresh instance each time: as an EF-owned value object it is keyed by its owning
    /// product, so a single shared instance must never back two products' <c>Details</c>.
    /// </summary>
    public static NutritionFacts Empty => new();

    public decimal? SaturatedFat { get; }

    public decimal? MonounsaturatedFat { get; }

    public decimal? PolyunsaturatedFat { get; }

    public decimal? TransFat { get; }

    public decimal? Sugars { get; }

    public decimal? Fiber { get; }

    public decimal? Salt { get; }

    public decimal? Sodium { get; }

    public decimal? Potassium { get; }

    public decimal? Calcium { get; }

    public decimal? Iron { get; }

    public decimal? VitaminA { get; }

    public decimal? VitaminC { get; }

    public decimal? VitaminD { get; }

    private static decimal? NonNegative(
        decimal? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is { } actual)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(actual, paramName);
        }

        return value;
    }
}
