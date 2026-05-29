using Jadlify.Application.Identity;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Products;
using Jadlify.Domain.Recipes;
using Jadlify.Infrastructure.Persistence;
using Jadlify.Infrastructure.Persistence.Repositories;
using Jadlify.SharedKernel;

namespace Jadlify.Infrastructure.Tests.Persistence;

public class RecipeRepositoryTests
{
    private static readonly ApplicationUserId OwnerId = new("user-owner");
    private static readonly ApplicationUserId OtherId = new("user-other");

    [Fact]
    public async Task GetByIdAsync_IncludesIngredients()
    {
        using SqliteTestDatabase database = new();
        var productId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository products = new(context, new TestCurrentUser(OwnerId));
            await products.AddAsync(new Product(productId, "Oats", new MacroNutrients(100m, 10m, 5m, 20m)));

            Recipe recipe = new(recipeId, "Porridge", 2);
            recipe.AddIngredient(new RecipeIngredient(productId, new GramAmount(150m)));
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            await recipes.AddAsync(recipe);
        }

        Recipe? loaded;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            loaded = await recipes.GetByIdAsync(recipeId);
        }

        Assert.NotNull(loaded);
        Assert.Single(loaded.Ingredients);
        Assert.Equal(productId, loaded.Ingredients[0].ProductId);
        Assert.Equal(150m, loaded.Ingredients[0].WholeRecipeAmount.Value);
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotReturnAnotherUsersRecipe()
    {
        using SqliteTestDatabase database = new();
        var recipeId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            await recipes.AddAsync(new Recipe(recipeId, "Porridge", 2));
        }

        Recipe? found;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OtherId));
            found = await recipes.GetByIdAsync(recipeId);
        }

        Assert.Null(found);
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyCurrentUsersRecipes()
    {
        using SqliteTestDatabase database = new();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository ownerRecipes = new(context, new TestCurrentUser(OwnerId));
            await ownerRecipes.AddAsync(new Recipe(Guid.NewGuid(), "Porridge", 2));
            await ownerRecipes.AddAsync(new Recipe(Guid.NewGuid(), "Pancakes", 4));

            RecipeRepository otherRecipes = new(context, new TestCurrentUser(OtherId));
            await otherRecipes.AddAsync(new Recipe(Guid.NewGuid(), "Salad", 1));
        }

        IReadOnlyList<Recipe> recipes;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository repository = new(context, new TestCurrentUser(OwnerId));
            recipes = await repository.ListAsync();
        }

        Assert.Equal(2, recipes.Count);
        Assert.All(recipes, recipe => Assert.Contains(recipe.Name, new[] { "Porridge", "Pancakes" }));
    }

    [Fact]
    public async Task DeleteAsync_ReturnsConflict_WhenRecipeUsedByMealPlan()
    {
        using SqliteTestDatabase database = new();
        var recipeId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            await recipes.AddAsync(new Recipe(recipeId, "Porridge", 2));

            MealPlanRepository mealPlans = new(context, new TestCurrentUser(OwnerId));
            await mealPlans.AddAsync(
                new MealPlanEntry(Guid.NewGuid(), new DateOnly(2026, 5, 28), recipeId, MealType.Breakfast, 1));
        }

        Result result;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            result = await recipes.DeleteAsync(recipeId);
        }

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Recipe.InUse", result.Error.Code);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRecipe_WhenNotReferenced()
    {
        using SqliteTestDatabase database = new();
        var recipeId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            await recipes.AddAsync(new Recipe(recipeId, "Porridge", 2));
        }

        Result result;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            result = await recipes.DeleteAsync(recipeId);
        }

        Recipe? remaining;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            remaining = await recipes.GetByIdAsync(recipeId);
        }

        Assert.True(result.IsSuccess);
        Assert.Null(remaining);
    }
}
