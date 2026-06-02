using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Recipes.ListRecipes;

public sealed class ListRecipesQueryHandler : IQueryHandler<ListRecipesQuery, IReadOnlyList<RecipeDto>>
{
    private readonly IRecipeRepository _recipes;

    public ListRecipesQueryHandler(IRecipeRepository recipes)
    {
        _recipes = recipes;
    }

    public async Task<Result<IReadOnlyList<RecipeDto>>> HandleAsync(
        ListRecipesQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Recipe> recipes = await _recipes.ListAsync(cancellationToken);

        return Result.Ok<IReadOnlyList<RecipeDto>>(recipes.Select(RecipeDto.FromDomain).ToList());
    }
}
