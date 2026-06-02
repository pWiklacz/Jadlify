using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Recipes.GetRecipe;

public sealed class GetRecipeQueryHandler : IQueryHandler<GetRecipeQuery, RecipeDto>
{
    private static readonly Error NotFound =
        Error.NotFound("Recipe.NotFound", "The recipe was not found for the current user.");

    private readonly IRecipeRepository _recipes;

    public GetRecipeQueryHandler(IRecipeRepository recipes)
    {
        _recipes = recipes;
    }

    public async Task<Result<RecipeDto>> HandleAsync(GetRecipeQuery query, CancellationToken cancellationToken)
    {
        Recipe? recipe = await _recipes.GetByIdAsync(query.Id, cancellationToken);

        return recipe is null
            ? Result.Fail<RecipeDto>(NotFound)
            : Result.Ok(RecipeDto.FromDomain(recipe));
    }
}
