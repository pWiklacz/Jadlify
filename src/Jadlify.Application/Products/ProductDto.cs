using Jadlify.Domain.Products;

namespace Jadlify.Application.Products;

/// <summary>
/// Read model for a user product: identity, optional barcode, the four per-100g macro
/// values, and the optional extended profile — net package size plus the per-100g fat
/// breakdown, sugars/fiber, salt/sodium/potassium, and micronutrients. The extended
/// fields are nullable because OFF coverage is sparse and manual entry is optional. Also
/// carries the optional brand and taxonomy <see cref="ProductCategory"/> (both nullable —
/// a null category is a valid "Bez kategorii" state). Returned by the list, get, and
/// catalog use-cases.
/// </summary>
public sealed record ProductDto(
    Guid Id,
    string Name,
    string? Barcode,
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbohydrates,
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
    string? Brand = null,
    ProductCategory? Category = null)
{
    public static ProductDto FromDomain(Product product) =>
        new(
            product.Id,
            product.Name,
            product.Barcode,
            product.Per100Grams.Calories,
            product.Per100Grams.Protein,
            product.Per100Grams.Fat,
            product.Per100Grams.Carbohydrates,
            product.PackageSizeGrams,
            product.Details.SaturatedFat,
            product.Details.MonounsaturatedFat,
            product.Details.PolyunsaturatedFat,
            product.Details.TransFat,
            product.Details.Sugars,
            product.Details.Fiber,
            product.Details.Salt,
            product.Details.Sodium,
            product.Details.Potassium,
            product.Details.Calcium,
            product.Details.Iron,
            product.Details.VitaminA,
            product.Details.VitaminC,
            product.Details.VitaminD,
            product.Brand,
            product.Category);
}
