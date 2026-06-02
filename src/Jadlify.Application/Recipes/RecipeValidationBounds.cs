namespace Jadlify.Application.Recipes;

public static class RecipeValidationBounds
{
    public const int MaxNameLength = 200;
    public const int MaxPortions = 1000;
    public const int MaxIngredients = 100;
    public const decimal MaxWholeRecipeGrams = 1_000_000m;
}
