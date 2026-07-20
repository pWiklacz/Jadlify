using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Products;

namespace Jadlify.Application.Products.UpdateProduct;

public sealed record UpdateProductCommand(
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
    ProductCategory? Category = null) : ICommand, IExtendedNutritionFields;
