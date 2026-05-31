using Jadlify.Domain.Products;

namespace Jadlify.Application.Products;

/// <summary>
/// Read model for a user product: identity, optional barcode, and the four
/// per-100g macro values. Returned by the list and get use-cases.
/// </summary>
public sealed record ProductDto(
    Guid Id,
    string Name,
    string? Barcode,
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbohydrates)
{
    public static ProductDto FromDomain(Product product) =>
        new(
            product.Id,
            product.Name,
            product.Barcode,
            product.Per100Grams.Calories,
            product.Per100Grams.Protein,
            product.Per100Grams.Fat,
            product.Per100Grams.Carbohydrates);
}
