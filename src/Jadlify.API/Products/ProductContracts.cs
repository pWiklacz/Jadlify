using Jadlify.Application.Products;

namespace Jadlify.API.Products;

/// <summary>
/// HTTP-facing product contracts, kept decoupled from the Application DTOs so the
/// wire shape can evolve independently of the use-case layer. Beyond the core macros
/// every contract carries the optional net package size and the extended per-100g
/// profile (fat breakdown, sugars/fiber, salt/sodium/potassium, micronutrients); all
/// extended fields are nullable because OFF coverage is sparse and manual entry is optional.
/// </summary>
public sealed record CreateProductRequest(
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
    decimal? VitaminD = null);

public sealed record UpdateProductRequest(
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
    decimal? VitaminD = null);

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string? Barcode,
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbohydrates,
    decimal? PackageSizeGrams,
    decimal? SaturatedFat,
    decimal? MonounsaturatedFat,
    decimal? PolyunsaturatedFat,
    decimal? TransFat,
    decimal? Sugars,
    decimal? Fiber,
    decimal? Salt,
    decimal? Sodium,
    decimal? Potassium,
    decimal? Calcium,
    decimal? Iron,
    decimal? VitaminA,
    decimal? VitaminC,
    decimal? VitaminD)
{
    public static ProductResponse FromDto(ProductDto dto) =>
        new(
            dto.Id,
            dto.Name,
            dto.Barcode,
            dto.Calories,
            dto.Protein,
            dto.Fat,
            dto.Carbohydrates,
            dto.PackageSizeGrams,
            dto.SaturatedFat,
            dto.MonounsaturatedFat,
            dto.PolyunsaturatedFat,
            dto.TransFat,
            dto.Sugars,
            dto.Fiber,
            dto.Salt,
            dto.Sodium,
            dto.Potassium,
            dto.Calcium,
            dto.Iron,
            dto.VitaminA,
            dto.VitaminC,
            dto.VitaminD);

    /// <summary>The 201 echo body: the persisted id plus the values the create request carried.</summary>
    public static ProductResponse Created(Guid id, CreateProductRequest request) =>
        new(
            id,
            request.Name,
            string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode,
            request.Calories,
            request.Protein,
            request.Fat,
            request.Carbohydrates,
            request.PackageSizeGrams,
            request.SaturatedFat,
            request.MonounsaturatedFat,
            request.PolyunsaturatedFat,
            request.TransFat,
            request.Sugars,
            request.Fiber,
            request.Salt,
            request.Sodium,
            request.Potassium,
            request.Calcium,
            request.Iron,
            request.VitaminA,
            request.VitaminC,
            request.VitaminD);
}

/// <summary>
/// Barcode-lookup response. <see cref="Outcome"/> is the string form of
/// <see cref="BarcodeLookupOutcome"/> (Found / NotFound / AlreadyInCatalog); all
/// pre-fill fields are optional so partial external data maps to blank form inputs.
/// </summary>
public sealed record BarcodeLookupResponse(
    string Outcome,
    string Barcode,
    Guid? ExistingProductId,
    string? Name,
    string? Brand,
    decimal? Calories,
    decimal? Protein,
    decimal? Fat,
    decimal? Carbohydrates,
    decimal? PackageSizeGrams,
    decimal? SaturatedFat,
    decimal? MonounsaturatedFat,
    decimal? PolyunsaturatedFat,
    decimal? TransFat,
    decimal? Sugars,
    decimal? Fiber,
    decimal? Salt,
    decimal? Sodium,
    decimal? Potassium,
    decimal? Calcium,
    decimal? Iron,
    decimal? VitaminA,
    decimal? VitaminC,
    decimal? VitaminD)
{
    public static BarcodeLookupResponse FromResult(BarcodeLookupResult result) =>
        new(
            result.Outcome.ToString(),
            result.Barcode,
            result.ExistingProductId,
            result.Name,
            result.Brand,
            result.Calories,
            result.Protein,
            result.Fat,
            result.Carbohydrates,
            result.PackageSizeGrams,
            result.SaturatedFat,
            result.MonounsaturatedFat,
            result.PolyunsaturatedFat,
            result.TransFat,
            result.Sugars,
            result.Fiber,
            result.Salt,
            result.Sodium,
            result.Potassium,
            result.Calcium,
            result.Iron,
            result.VitaminA,
            result.VitaminC,
            result.VitaminD);
}
