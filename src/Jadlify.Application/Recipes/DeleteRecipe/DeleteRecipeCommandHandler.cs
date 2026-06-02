using Jadlify.Application.Common.Mediator;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Recipes.DeleteRecipe;

public sealed class DeleteRecipeCommandHandler : ICommandHandler<DeleteRecipeCommand>
{
    private readonly IRecipeRepository _recipes;

    public DeleteRecipeCommandHandler(IRecipeRepository recipes)
    {
        _recipes = recipes;
    }

    public Task<Result> HandleAsync(DeleteRecipeCommand command, CancellationToken cancellationToken) =>
        _recipes.DeleteAsync(command.Id, cancellationToken);
}
