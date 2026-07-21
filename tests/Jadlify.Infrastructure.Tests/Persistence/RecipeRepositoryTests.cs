using Jadlify.Application.Identity;
using Jadlify.Application.Recipes;
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
            recipe.AddIngredient(new RecipeIngredient(
                productId,
                "Oats",
                new MacroNutrients(100m, 10m, 5m, 20m),
                new GramAmount(150m)));
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
        Assert.Equal("Oats", loaded.Ingredients[0].ProductName);
        Assert.Equal(100m, loaded.Ingredients[0].Per100Grams.Calories);
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
    public async Task GetCatalogAsync_PagesOwnerScopedRecipesWithIngredients()
    {
        using SqliteTestDatabase database = new();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository ownerRecipes = new(context, new TestCurrentUser(OwnerId));
            await ownerRecipes.AddAsync(BuildRecipe("Apple pie", 2, 100m, 200m));
            await ownerRecipes.AddAsync(BuildRecipe("Banana bread", 2, 100m, 200m));
            await ownerRecipes.AddAsync(BuildRecipe("Cherry tart", 2, 100m, 200m));

            RecipeRepository otherRecipes = new(context, new TestCurrentUser(OtherId));
            await otherRecipes.AddAsync(BuildRecipe("Apple strudel", 2, 100m, 200m));
        }

        RecipeCatalogResult page;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository repository = new(context, new TestCurrentUser(OwnerId));
            page = await repository.GetCatalogAsync(null, RecipeCatalogSort.NameAsc, skip: 0, take: 2);
        }

        Assert.Equal(3, page.Total);
        Assert.Equal(new[] { "Apple pie", "Banana bread" }, page.Items.Select(r => r.Name).ToArray());
        // Ingredients must be loaded so the shared macro core can compute the summaries.
        Assert.All(page.Items, recipe => Assert.Single(recipe.Ingredients));
    }

    [Fact]
    public async Task GetCatalogAsync_OrdersByCaloriesPerServing_BeforePaging()
    {
        using SqliteTestDatabase database = new();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            // Per-serving kcal: Alpha 400, Beta 100, Gamma 200 — the inverse of alphabetical
            // order, so a window taken by name and then re-sorted would return the wrong rows.
            await recipes.AddAsync(BuildRecipe("Alpha", 1, 100m, 400m));
            await recipes.AddAsync(BuildRecipe("Beta", 1, 100m, 100m));
            await recipes.AddAsync(BuildRecipe("Gamma", 1, 100m, 200m));
        }

        RecipeCatalogResult page;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository repository = new(context, new TestCurrentUser(OwnerId));
            page = await repository.GetCatalogAsync(
                null, RecipeCatalogSort.CaloriesPerServingAsc, skip: 0, take: 2);
        }

        Assert.Equal(3, page.Total);
        Assert.Equal(new[] { "Beta", "Gamma" }, page.Items.Select(r => r.Name).ToArray());
    }

    [Fact]
    public async Task GetCatalogAsync_FiltersByNameSearch_CaseInsensitively()
    {
        using SqliteTestDatabase database = new();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            await recipes.AddAsync(BuildRecipe("Owsianka", 1, 100m, 100m));
            await recipes.AddAsync(BuildRecipe("Kurczak", 1, 100m, 100m));
        }

        RecipeCatalogResult page;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository repository = new(context, new TestCurrentUser(OwnerId));
            page = await repository.GetCatalogAsync(
                "KURCZ", RecipeCatalogSort.NameAsc, skip: 0, take: 10);
        }

        Assert.Equal(1, page.Total);
        Assert.Equal("Kurczak", Assert.Single(page.Items).Name);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesMetadataAndIngredientComposition()
    {
        using SqliteTestDatabase database = new();
        var recipeId = Guid.NewGuid();
        var oatsId = Guid.NewGuid();
        var milkId = Guid.NewGuid();
        var honeyId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            Recipe recipe = new(recipeId, "Porridge", 2);
            recipe.AddIngredient(new RecipeIngredient(
                oatsId,
                "Oats",
                new MacroNutrients(100m, 10m, 5m, 20m),
                new GramAmount(150m)));
            recipe.AddIngredient(new RecipeIngredient(
                milkId,
                "Milk",
                new MacroNutrients(64m, 3.3m, 3.6m, 4.8m),
                new GramAmount(200m)));

            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            await recipes.AddAsync(recipe);
        }

        Result result;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            Recipe replacement = new(recipeId, "Sweet porridge", 3);
            replacement.ReplaceDetails(
                "Sweet porridge",
                3,
                [
                    new RecipeIngredient(
                        oatsId,
                        "Oat flakes",
                        new MacroNutrients(110m, 11m, 6m, 21m),
                        new GramAmount(175m)),
                    new RecipeIngredient(
                        honeyId,
                        "Honey",
                        new MacroNutrients(304m, 0m, 0m, 82m),
                        new GramAmount(25m)),
                ]);

            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            result = await recipes.UpdateAsync(replacement);
        }

        Recipe? loaded;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            loaded = await recipes.GetByIdAsync(recipeId);
        }

        Assert.True(result.IsSuccess);
        Assert.NotNull(loaded);
        Assert.Equal("Sweet porridge", loaded!.Name);
        Assert.Equal(3, loaded.Portions);
        Assert.Equal(2, loaded.Ingredients.Count);
        Assert.DoesNotContain(loaded.Ingredients, ingredient => ingredient.ProductId == milkId);

        RecipeIngredient oats = loaded.Ingredients.Single(ingredient => ingredient.ProductId == oatsId);
        Assert.Equal("Oat flakes", oats.ProductName);
        Assert.Equal(110m, oats.Per100Grams.Calories);
        Assert.Equal(175m, oats.WholeRecipeAmount.Value);

        RecipeIngredient honey = loaded.Ingredients.Single(ingredient => ingredient.ProductId == honeyId);
        Assert.Equal("Honey", honey.ProductName);
        Assert.Equal(25m, honey.WholeRecipeAmount.Value);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotMutateAnotherUsersRecipe()
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
            Recipe replacement = new(recipeId, "Hijacked", 1);
            replacement.AddIngredient(new RecipeIngredient(
                Guid.NewGuid(),
                "Honey",
                new MacroNutrients(304m, 0m, 0m, 82m),
                new GramAmount(25m)));

            RecipeRepository recipes = new(context, new TestCurrentUser(OtherId));
            result = await recipes.UpdateAsync(replacement);
        }

        Recipe? loaded;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            loaded = await recipes.GetByIdAsync(recipeId);
        }

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.NotNull(loaded);
        Assert.Equal("Porridge", loaded!.Name);
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
                MealPlanEntry.ForRecipe(Guid.NewGuid(), new DateOnly(2026, 5, 28), recipeId, MealType.Breakfast, 1));
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

    [Fact]
    public async Task DeleteAsync_DoesNotDeleteAnotherUsersRecipe()
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
            RecipeRepository recipes = new(context, new TestCurrentUser(OtherId));
            result = await recipes.DeleteAsync(recipeId);
        }

        Recipe? remaining;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            remaining = await recipes.GetByIdAsync(recipeId);
        }

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.NotNull(remaining);
    }

    [Fact]
    public async Task ListByIdsAsync_ReturnsOnlyRequestedCurrentUserRecipes()
    {
        using SqliteTestDatabase database = new();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var thirdId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            await recipes.AddAsync(new Recipe(firstId, "Porridge", 2));
            await recipes.AddAsync(new Recipe(secondId, "Pancakes", 4));
            await recipes.AddAsync(new Recipe(thirdId, "Omelette", 1));
        }

        IReadOnlyList<Recipe> found;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            found = await recipes.ListByIdsAsync(new[] { firstId, thirdId });
        }

        Assert.Equal(2, found.Count);
        Assert.Contains(found, recipe => recipe.Id == firstId);
        Assert.Contains(found, recipe => recipe.Id == thirdId);
        Assert.DoesNotContain(found, recipe => recipe.Id == secondId);
    }

    [Fact]
    public async Task ListByIdsAsync_DoesNotReturnAnotherUsersRecipe()
    {
        using SqliteTestDatabase database = new();
        var ownerRecipeId = Guid.NewGuid();
        var otherRecipeId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository owner = new(context, new TestCurrentUser(OwnerId));
            await owner.AddAsync(new Recipe(ownerRecipeId, "Porridge", 2));
            RecipeRepository other = new(context, new TestCurrentUser(OtherId));
            await other.AddAsync(new Recipe(otherRecipeId, "Salad", 1));
        }

        IReadOnlyList<Recipe> found;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            found = await recipes.ListByIdsAsync(new[] { ownerRecipeId, otherRecipeId });
        }

        Recipe single = Assert.Single(found);
        Assert.Equal(ownerRecipeId, single.Id);
    }

    [Fact]
    public async Task ListByIdsAsync_ReturnsEmpty_WhenNoIdsRequested()
    {
        using SqliteTestDatabase database = new();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            await recipes.AddAsync(new Recipe(Guid.NewGuid(), "Porridge", 2));
        }

        IReadOnlyList<Recipe> found;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            found = await recipes.ListByIdsAsync(Array.Empty<Guid>());
        }

        Assert.Empty(found);
    }

    /// <summary>One-ingredient recipe whose per-serving calories are <c>calories * grams / 100 / portions</c>.</summary>
    private static Recipe BuildRecipe(string name, int portions, decimal calories, decimal grams)
    {
        Recipe recipe = new(Guid.NewGuid(), name, portions);
        recipe.AddIngredient(new RecipeIngredient(
            Guid.NewGuid(),
            "Product",
            new MacroNutrients(calories, 10m, 5m, 20m),
            new GramAmount(grams)));

        return recipe;
    }
}
