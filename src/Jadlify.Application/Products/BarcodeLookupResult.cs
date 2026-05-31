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
/// Result of looking a barcode up. Macro/name fields are nullable so partial
/// external data maps to blank form fields the user can complete.
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
    decimal? Carbohydrates = null)
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
            Carbohydrates: data.Carbohydrates);

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
            Carbohydrates: existing.Carbohydrates);
}
