using Jadlify.Application.Recipes;
using Jadlify.Application.Recipes.UpdateRecipe;

namespace Jadlify.Application.Tests.Recipes;

public class UpdateRecipeCommandValidatorTests
{
    private readonly UpdateRecipeCommandValidator _validator = new();

    [Fact]
    public void Rejects_EmptyRecipeId()
    {
        var command = new UpdateRecipeCommand(
            Guid.Empty,
            "Porridge",
            4,
            [new RecipeIngredientInput(Guid.NewGuid(), 150m)]);

        Assert.False(_validator.Validate(command).IsValid);
    }
}
