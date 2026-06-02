using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Recipes.DeleteRecipe;

public sealed record DeleteRecipeCommand(Guid Id) : ICommand;
