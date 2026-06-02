using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Recipes;

namespace Jadlify.Domain.Tests.Recipes;

public class RecipeTests
{
    [Fact]
    public void ReplaceDetails_ReplacesMetadataAndIngredients()
    {
        var recipe = new Recipe(Guid.NewGuid(), "Old", portions: 1);
        RecipeIngredient ingredient = Ingredient(Guid.NewGuid(), "Rice", 200m);

        recipe.ReplaceDetails("New", portions: 4, [ingredient]);

        Assert.Equal("New", recipe.Name);
        Assert.Equal(4, recipe.Portions);
        Assert.Same(ingredient, Assert.Single(recipe.Ingredients));
    }

    [Fact]
    public void ReplaceDetails_RejectsEmptyIngredientSet()
    {
        var recipe = new Recipe(Guid.NewGuid(), "Soup", portions: 1);

        Assert.Throws<InvalidOperationException>(
            () => recipe.ReplaceDetails("Soup", portions: 1, []));
    }

    [Fact]
    public void ReplaceDetails_RejectsDuplicateProductIngredients()
    {
        var productId = Guid.NewGuid();
        var recipe = new Recipe(Guid.NewGuid(), "Double", portions: 1);

        Assert.Throws<InvalidOperationException>(
            () => recipe.ReplaceDetails(
                "Double",
                portions: 1,
                [Ingredient(productId, "Rice", 100m), Ingredient(productId, "Rice", 50m)]));
    }

    private static RecipeIngredient Ingredient(Guid productId, string productName, decimal grams) =>
        new(
            productId,
            productName,
            new MacroNutrients(100m, 5m, 2m, 20m),
            new GramAmount(grams));
}
