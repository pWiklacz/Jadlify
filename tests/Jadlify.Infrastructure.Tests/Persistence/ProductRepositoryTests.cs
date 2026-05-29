using Jadlify.Application.Identity;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Products;
using Jadlify.Domain.Recipes;
using Jadlify.Infrastructure.Persistence;
using Jadlify.Infrastructure.Persistence.Repositories;
using Jadlify.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Jadlify.Infrastructure.Tests.Persistence;

public class ProductRepositoryTests
{
    private static readonly ApplicationUserId OwnerId = new("user-owner");
    private static readonly ApplicationUserId OtherId = new("user-other");

    [Fact]
    public async Task AddAsync_StampsCurrentUserAsOwner()
    {
        using SqliteTestDatabase database = new();
        var productId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            await repository.AddAsync(NewProduct(productId, "Oats"));
        }

        string? owner;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            owner = await context.Products
                .Where(product => product.Id == productId)
                .Select(product => EF.Property<string>(product, "UserId"))
                .SingleAsync();
        }

        Assert.Equal(OwnerId.Value, owner);
    }

    [Fact]
    public async Task AddAsync_PersistsMacrosWithDecimalScale()
    {
        using SqliteTestDatabase database = new();
        var productId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            await repository.AddAsync(
                new Product(productId, "Oats", new MacroNutrients(123.45m, 6.70m, 8.90m, 12.34m)));
        }

        Product? stored;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            stored = await repository.GetByIdAsync(productId);
        }

        Assert.NotNull(stored);
        Assert.Equal(123.45m, stored.Per100Grams.Calories);
        Assert.Equal(12.34m, stored.Per100Grams.Carbohydrates);
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotReturnAnotherUsersProduct()
    {
        using SqliteTestDatabase database = new();
        var productId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            await repository.AddAsync(NewProduct(productId, "Oats"));
        }

        Product? found;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OtherId));
            found = await repository.GetByIdAsync(productId);
        }

        Assert.Null(found);
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyCurrentUsersProducts()
    {
        using SqliteTestDatabase database = new();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository ownerRepository = new(context, new TestCurrentUser(OwnerId));
            await ownerRepository.AddAsync(NewProduct(Guid.NewGuid(), "Oats"));
            await ownerRepository.AddAsync(NewProduct(Guid.NewGuid(), "Rice"));

            ProductRepository otherRepository = new(context, new TestCurrentUser(OtherId));
            await otherRepository.AddAsync(NewProduct(Guid.NewGuid(), "Beans"));
        }

        IReadOnlyList<Product> products;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            products = await repository.ListAsync();
        }

        Assert.Equal(2, products.Count);
        Assert.All(products, product => Assert.Contains(product.Name, new[] { "Oats", "Rice" }));
    }

    [Fact]
    public async Task DeleteAsync_ReturnsConflict_WhenProductUsedByRecipe()
    {
        using SqliteTestDatabase database = new();
        var productId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository products = new(context, new TestCurrentUser(OwnerId));
            await products.AddAsync(NewProduct(productId, "Oats"));

            Recipe recipe = new(Guid.NewGuid(), "Porridge", 2);
            recipe.AddIngredient(new RecipeIngredient(productId, new GramAmount(150m)));
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            await recipes.AddAsync(recipe);
        }

        Result result;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository products = new(context, new TestCurrentUser(OwnerId));
            result = await products.DeleteAsync(productId);
        }

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Product.InUse", result.Error.Code);
    }

    [Fact]
    public async Task DeleteAsync_RemovesProduct_WhenNotReferenced()
    {
        using SqliteTestDatabase database = new();
        var productId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            await repository.AddAsync(NewProduct(productId, "Oats"));
        }

        Result result;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            result = await repository.DeleteAsync(productId);
        }

        Product? remaining;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            remaining = await repository.GetByIdAsync(productId);
        }

        Assert.True(result.IsSuccess);
        Assert.Null(remaining);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNotFound_WhenProductBelongsToAnotherUser()
    {
        using SqliteTestDatabase database = new();
        var productId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            await repository.AddAsync(NewProduct(productId, "Oats"));
        }

        Result result;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OtherId));
            result = await repository.DeleteAsync(productId);
        }

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    private static Product NewProduct(Guid id, string name)
    {
        return new Product(id, name, new MacroNutrients(100m, 10m, 5m, 20m));
    }
}
