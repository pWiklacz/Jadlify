using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Products;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Products.CreateProduct;

public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _products;

    public CreateProductCommandHandler(IProductRepository products)
    {
        _products = products;
    }

    public async Task<Result<Guid>> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var macros = new MacroNutrients(
            command.Calories,
            command.Protein,
            command.Fat,
            command.Carbohydrates);
        var details = command.ToNutritionFacts();
        string? barcode = string.IsNullOrWhiteSpace(command.Barcode) ? null : command.Barcode;
        string? brand = string.IsNullOrWhiteSpace(command.Brand) ? null : command.Brand.Trim();

        var product = new Product(
            id, command.Name, macros, barcode, command.PackageSizeGrams, details, brand, command.Category);
        await _products.AddAsync(product, cancellationToken);

        return Result.Ok(id);
    }
}
