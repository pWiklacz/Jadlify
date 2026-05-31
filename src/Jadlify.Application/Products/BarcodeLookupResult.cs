namespace Jadlify.Application.Products;

/// <summary>
/// The outcome of a barcode lookup. None of these is an error — a miss simply
/// routes the user into manual entry (FR-006).
/// </summary>
public enum BarcodeLookupOutcome
{
    /// <summary>External source returned data to pre-fill the form.</summary>
    Found,

    /// <summary>No data anywhere; keep the barcode and let the user fill the form.</summary>
    NotFound,

    /// <summary>The user already owns a product with this barcode.</summary>
    AlreadyInCatalog,
}

/// <summary>
/// Result of looking a barcode up. Every value field is nullable so partial external
/// data maps to blank form fields the user can complete: the core macros plus the
/// optional package size and extended per-100g profile (fat breakdown, sugars/fiber,
/// salt/sodium/potassium, and micronutrients).
/// </summary>
public sealed record BarcodeLookupResult(
    BarcodeLookupOutcome Outcome,
    string Barcode,
    Guid? ExistingProductId = null,
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
    decimal? VitaminD = null)
{
    public static BarcodeLookupResult NotFound(string barcode) =>
        new(BarcodeLookupOutcome.NotFound, barcode);

    public static BarcodeLookupResult Found(string barcode, BarcodeProductData data) =>
        new(
            BarcodeLookupOutcome.Found,
            barcode,
            ExistingProductId: null,
            Name: data.Name,
            Brand: data.Brand,
            Calories: data.Calories,
            Protein: data.Protein,
            Fat: data.Fat,
            Carbohydrates: data.Carbohydrates,
            PackageSizeGrams: data.PackageSizeGrams,
            SaturatedFat: data.SaturatedFat,
            MonounsaturatedFat: data.MonounsaturatedFat,
            PolyunsaturatedFat: data.PolyunsaturatedFat,
            TransFat: data.TransFat,
            Sugars: data.Sugars,
            Fiber: data.Fiber,
            Salt: data.Salt,
            Sodium: data.Sodium,
            Potassium: data.Potassium,
            Calcium: data.Calcium,
            Iron: data.Iron,
            VitaminA: data.VitaminA,
            VitaminC: data.VitaminC,
            VitaminD: data.VitaminD);

    public static BarcodeLookupResult AlreadyInCatalog(string barcode, ProductDto existing) =>
        new(
            BarcodeLookupOutcome.AlreadyInCatalog,
            barcode,
            ExistingProductId: existing.Id,
            Name: existing.Name,
            Brand: null,
            Calories: existing.Calories,
            Protein: existing.Protein,
            Fat: existing.Fat,
            Carbohydrates: existing.Carbohydrates,
            PackageSizeGrams: existing.PackageSizeGrams,
            SaturatedFat: existing.SaturatedFat,
            MonounsaturatedFat: existing.MonounsaturatedFat,
            PolyunsaturatedFat: existing.PolyunsaturatedFat,
            TransFat: existing.TransFat,
            Sugars: existing.Sugars,
            Fiber: existing.Fiber,
            Salt: existing.Salt,
            Sodium: existing.Sodium,
            Potassium: existing.Potassium,
            Calcium: existing.Calcium,
            Iron: existing.Iron,
            VitaminA: existing.VitaminA,
            VitaminC: existing.VitaminC,
            VitaminD: existing.VitaminD);
}
