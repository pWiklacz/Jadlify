using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Recipes;
using Jadlify.Domain.Planning;
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

    private readonly IMealPlanRepository _mealPlans;
    private readonly IRecipeRepository _recipes;

    public AddMealPlanEntryCommandHandler(IMealPlanRepository mealPlans, IRecipeRepository recipes)
    {
        _mealPlans = mealPlans;
        _recipes = recipes;
    }

    public async Task<Result<Guid>> HandleAsync(AddMealPlanEntryCommand command, CancellationToken cancellationToken)
    {
        // Owner-scoped existence check: a missing or cross-user recipe resolves to null,
        // so an entry can never reference another user's recipe. Duplicate entries are allowed.
        Recipe? recipe = await _recipes.GetByIdAsync(command.RecipeId, cancellationToken);
        if (recipe is null)
        {
            return Result.Fail<Guid>(RecipeNotFound);
        }

        var entry = new MealPlanEntry(
            Guid.NewGuid(),
            command.Date,
            command.RecipeId,
            command.MealType,
            command.Portions);

        await _mealPlans.AddAsync(entry, cancellationToken);

        return Result.Ok(entry.Id);
    }
}
