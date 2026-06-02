using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Recipes.ListRecipes;

public sealed record ListRecipesQuery : IQuery<IReadOnlyList<RecipeDto>>;
