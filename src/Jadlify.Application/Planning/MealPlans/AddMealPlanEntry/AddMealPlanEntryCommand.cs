using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Planning;

namespace Jadlify.Application.Planning.MealPlans.AddMealPlanEntry;

/// <summary>
/// Adds one meal to a day. Exactly one source variant must be supplied:
/// <see cref="RecipeId"/> with <see cref="Portions"/> (positive multiples of 0.5), or
/// <see cref="ProductId"/> with <see cref="Grams"/>. The variants are separate fields rather
/// than one generic quantity so the unit is never inferred.
/// </summary>
public sealed record AddMealPlanEntryCommand(
    DateOnly Date,
    MealType MealType,
    Guid? RecipeId = null,
    decimal? Portions = null,
    Guid? ProductId = null,
    decimal? Grams = null) : ICommand<Guid>
{
    public bool IsRecipeSource => RecipeId is not null || Portions is not null;

    public bool IsProductSource => ProductId is not null || Grams is not null;
}
