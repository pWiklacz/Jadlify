using Jadlify.Application.Products;

namespace Jadlify.API.Products;

/// <summary>
/// HTTP-facing product contracts, kept decoupled from the Application DTOs so the
/// wire shape can evolve independently of the use-case layer.
/// </summary>
public sealed record CreateProductRequest(
    string Name,
    string? Barcode,
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbohydrates);

public sealed record UpdateProductRequest(
    string Name,
    string? Barcode,
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbohydrates);

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string? Barcode,
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbohydrates)
{
    public static ProductResponse FromDto(ProductDto dto) =>
        new(dto.Id, dto.Name, dto.Barcode, dto.Calories, dto.Protein, dto.Fat, dto.Carbohydrates);
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
    decimal? Carbohydrates)
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
            result.Carbohydrates);
}
