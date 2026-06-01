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

    public void ReplaceDetails(string name, int portions, IEnumerable<RecipeIngredient> ingredients)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(portions);
        ArgumentNullException.ThrowIfNull(ingredients);

        var replacement = ingredients.ToList();
        if (replacement.Count == 0)
        {
            throw new InvalidOperationException($"Recipe {Id} must have at least one ingredient.");
        }

        Guid? duplicateProductId = replacement
            .GroupBy(ingredient => ingredient.ProductId)
            .Where(group => group.Count() > 1)
            .Select(group => (Guid?)group.Key)
            .FirstOrDefault();
        if (duplicateProductId is { } productId)
        {
            throw new InvalidOperationException(
                $"Product {productId} is already an ingredient of recipe {Id}.");
        }

        Name = name;
        Portions = portions;
        _ingredients.Clear();
        _ingredients.AddRange(replacement);
    }
}
