using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Recipes;

namespace Jadlify.Domain.Tests.Recipes;

public class RecipeIngredientTests
{
    [Fact]
    public void Constructor_StoresProductSnapshot()
    {
        var productId = Guid.NewGuid();
        var macros = new MacroNutrients(63m, 11m, 0.2m, 4m);

        var ingredient = new RecipeIngredient(productId, "Skyr", macros, new GramAmount(150m));

        Assert.Equal(productId, ingredient.ProductId);
        Assert.Equal("Skyr", ingredient.ProductName);
        Assert.Equal(macros, ingredient.Per100Grams);
        Assert.Equal(150m, ingredient.WholeRecipeAmount.Value);
    }

    [Fact]
    public void Constructor_RejectsBlankProductName()
    {
        Assert.Throws<ArgumentException>(
            () => new RecipeIngredient(
                Guid.NewGuid(),
                "",
                new MacroNutrients(63m, 11m, 0.2m, 4m),
                new GramAmount(150m)));
    }

    [Fact]
    public void Constructor_RejectsMissingMacros()
    {
        Assert.Throws<ArgumentNullException>(
            () => new RecipeIngredient(Guid.NewGuid(), "Skyr", null!, new GramAmount(150m)));
    }
}
