using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Products.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Barcode,
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbohydrates) : ICommand;
