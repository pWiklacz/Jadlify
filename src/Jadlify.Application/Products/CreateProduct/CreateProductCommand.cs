using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Products.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string? Barcode,
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbohydrates) : ICommand<Guid>;
