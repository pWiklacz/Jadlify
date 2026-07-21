using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Products;
using Jadlify.Application.Recipes;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Products;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Planning.MealPlans.AddMealPlanEntry;

public sealed class AddMealPlanEntryCommandHandler : ICommandHandler<AddMealPlanEntryCommand, Guid>
{
    private static readonly Error RecipeNotFound = new ValidationError(
    [
        new Error(
            "RecipeId",
            "The recipe was not found for the current user.",
            ErrorType.Validation)
    ]);

    private static readonly Error ProductNotFound = new ValidationError(
    [
        new Error(
            "ProductId",
            "The product was not found for the current user.",
            ErrorType.Validation)
    ]);

    private readonly IMealPlanRepository _mealPlans;
    private readonly IRecipeRepository _recipes;
    private readonly IProductRepository _products;

    public AddMealPlanEntryCommandHandler(
        IMealPlanRepository mealPlans,
        IRecipeRepository recipes,
        IProductRepository products)
    {
        _mealPlans = mealPlans;
        _recipes = recipes;
        _products = products;
    }

    public async Task<Result<Guid>> HandleAsync(AddMealPlanEntryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Result<MealPlanEntry> entry = command.IsProductSource
            ? await BuildProductEntryAsync(command, cancellationToken)
            : await BuildRecipeEntryAsync(command, cancellationToken);

        if (entry.IsFailure)
        {
            return Result.Fail<Guid>(entry.Error);
        }

        await _mealPlans.AddAsync(entry.Value, cancellationToken);

        return Result.Ok(entry.Value.Id);
    }

    private async Task<Result<MealPlanEntry>> BuildRecipeEntryAsync(
        AddMealPlanEntryCommand command,
        CancellationToken cancellationToken)
    {
        // Owner-scoped existence check: a missing or cross-user recipe resolves to null,
        // so an entry can never reference another user's recipe. Duplicate entries are allowed.
        Recipe? recipe = await _recipes.GetByIdAsync(command.RecipeId!.Value, cancellationToken);
        if (recipe is null)
        {
            return Result.Fail<MealPlanEntry>(RecipeNotFound);
        }

        return Result.Ok(MealPlanEntry.ForRecipe(
            Guid.NewGuid(),
            command.Date,
            recipe.Id,
            command.MealType,
            command.Portions!.Value));
    }

    private async Task<Result<MealPlanEntry>> BuildProductEntryAsync(
        AddMealPlanEntryCommand command,
        CancellationToken cancellationToken)
    {
        // Same owner-scoped check as recipes, then the catalog state is copied into the entry:
        // from here on the planned meal is independent of later product edits or deletion.
        Product? product = await _products.GetByIdAsync(command.ProductId!.Value, cancellationToken);
        if (product is null)
        {
            return Result.Fail<MealPlanEntry>(ProductNotFound);
        }

        return Result.Ok(MealPlanEntry.ForProduct(
            Guid.NewGuid(),
            command.Date,
            PlannedProductSnapshot.FromProduct(product),
            command.MealType,
            command.Grams!.Value));
    }
}
