namespace Jadlify.Domain.Recipes;

public sealed class Recipe
{
    private readonly List<RecipeIngredient> _ingredients = new();

    public Recipe(Guid id, string name, int portions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(portions);

        Id = id;
        Name = name;
        Portions = portions;
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public int Portions { get; private set; }

    public IReadOnlyList<RecipeIngredient> Ingredients => _ingredients;

    public void AddIngredient(RecipeIngredient ingredient)
    {
        ArgumentNullException.ThrowIfNull(ingredient);

        if (_ingredients.Any(existing => existing.ProductId == ingredient.ProductId))
        {
            throw new InvalidOperationException(
                $"Product {ingredient.ProductId} is already an ingredient of recipe {Id}.");
        }

        _ingredients.Add(ingredient);
    }
}
