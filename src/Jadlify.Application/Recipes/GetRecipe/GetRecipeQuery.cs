using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Recipes.GetRecipe;

public sealed record GetRecipeQuery(Guid Id) : IQuery<RecipeDto>;
