using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Products;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Products.UpdateProduct;

public sealed class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand>
{
    private readonly IProductRepository _products;

    public UpdateProductCommandHandler(IProductRepository products)
    {
        _products = products;
    }

    public Task<Result> HandleAsync(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var macros = new MacroNutrients(
            command.Calories,
            command.Protein,
            command.Fat,
            command.Carbohydrates);
        var details = command.ToNutritionFacts();
        string? barcode = string.IsNullOrWhiteSpace(command.Barcode) ? null : command.Barcode;

        var product = new Product(command.Id, command.Name, macros, barcode, command.PackageSizeGrams, details);

        // The repository scopes to the current user and returns NotFound when the
        // product does not belong to them; forward that Result unchanged.
        return _products.UpdateAsync(product, cancellationToken);
    }
}
