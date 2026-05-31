using Jadlify.Domain.Nutrition;

namespace Jadlify.Application.Products;

/// <summary>
/// The 14 optional per-100g extended nutrient fields, shared by the create and update
/// commands so the mapping into a <see cref="NutritionFacts"/> value object lives in one
/// place. A record whose component names match these members satisfies the interface
/// automatically.
/// </summary>
public interface IExtendedNutritionFields
{
    decimal? SaturatedFat { get; }

    decimal? MonounsaturatedFat { get; }

    decimal? PolyunsaturatedFat { get; }

    decimal? TransFat { get; }

    decimal? Sugars { get; }

    decimal? Fiber { get; }

    decimal? Salt { get; }

    decimal? Sodium { get; }

    decimal? Potassium { get; }

    decimal? Calcium { get; }

    decimal? Iron { get; }

    decimal? VitaminA { get; }

    decimal? VitaminC { get; }

    decimal? VitaminD { get; }
}

public static class ExtendedNutritionFieldsExtensions
{
    /// <summary>Maps the flat command fields into the domain <see cref="NutritionFacts"/> value object.</summary>
    public static NutritionFacts ToNutritionFacts(this IExtendedNutritionFields fields) =>
        new(
            saturatedFat: fields.SaturatedFat,
            monounsaturatedFat: fields.MonounsaturatedFat,
            polyunsaturatedFat: fields.PolyunsaturatedFat,
            transFat: fields.TransFat,
            sugars: fields.Sugars,
            fiber: fields.Fiber,
            salt: fields.Salt,
            sodium: fields.Sodium,
            potassium: fields.Potassium,
            calcium: fields.Calcium,
            iron: fields.Iron,
            vitaminA: fields.VitaminA,
            vitaminC: fields.VitaminC,
            vitaminD: fields.VitaminD);
}
