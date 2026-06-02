using Jadlify.Application.Recipes;
using Jadlify.Application.Recipes.CreateRecipe;

namespace Jadlify.Application.Tests.Recipes;

public class CreateRecipeCommandValidatorTests
{
    private readonly CreateRecipeCommandValidator _validator = new();

    private static CreateRecipeCommand Valid(
        string name = "Porridge",
        int portions = 4,
        IReadOnlyList<RecipeIngredientInput>? ingredients = null) =>
        new(name, portions, ingredients ?? [new RecipeIngredientInput(Guid.NewGuid(), 150m)]);

    [Fact]
    public void Accepts_CompleteRecipeInput()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    [Fact]
    public void Rejects_BlankName()
    {
        Assert.False(_validator.Validate(Valid(name: "")).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rejects_NonPositivePortions(int portions)
    {
        Assert.False(_validator.Validate(Valid(portions: portions)).IsValid);
    }

    [Fact]
    public void Rejects_EmptyIngredients()
    {
        Assert.False(_validator.Validate(Valid(ingredients: [])).IsValid);
    }

    [Fact]
    public void Rejects_DuplicateProducts()
    {
        var productId = Guid.NewGuid();

        Assert.False(_validator.Validate(Valid(ingredients:
        [
            new RecipeIngredientInput(productId, 100m),
            new RecipeIngredientInput(productId, 50m)
        ])).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rejects_NonPositiveWholeRecipeGrams(decimal grams)
    {
        Assert.False(_validator.Validate(Valid(ingredients: [new RecipeIngredientInput(Guid.NewGuid(), grams)])).IsValid);
    }

    [Fact]
    public void Rejects_EmptyProductId()
    {
        Assert.False(_validator.Validate(Valid(ingredients: [new RecipeIngredientInput(Guid.Empty, 100m)])).IsValid);
    }
}
