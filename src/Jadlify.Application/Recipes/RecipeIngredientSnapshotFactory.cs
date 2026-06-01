using Jadlify.Application.Products;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Products;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Recipes;

internal static class RecipeIngredientSnapshotFactory
{
    private static readonly Error MissingIngredientProduct = new ValidationError(
    [
        new Error(
            "Ingredients",
            "One or more ingredient products were not found for the current user.",
            ErrorType.Validation)
    ]);

    public static async Task<Result<IReadOnlyList<RecipeIngredient>>> CreateSnapshotsAsync(
        IReadOnlyList<RecipeIngredientInput> inputs,
        IProductRepository products,
        CancellationToken cancellationToken)
    {
        Guid[] productIds = inputs.Select(input => input.ProductId).Distinct().ToArray();
        IReadOnlyList<Product> loadedProducts = await products.ListByIdsAsync(productIds, cancellationToken);
        var productsById = loadedProducts.ToDictionary(product => product.Id);

        if (productsById.Count != productIds.Length)
        {
            return Result.Fail<IReadOnlyList<RecipeIngredient>>(MissingIngredientProduct);
        }

        var ingredients = inputs
            .Select(input =>
            {
                Product product = productsById[input.ProductId];

                return new RecipeIngredient(
                    product.Id,
                    product.Name,
                    product.Per100Grams,
                    new GramAmount(input.WholeRecipeGrams));
            })
            .ToList();

        return Result.Ok<IReadOnlyList<RecipeIngredient>>(ingredients);
    }
}
