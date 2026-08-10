using Jadlify.Application.Identity;
using Jadlify.Application.Products;
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
    public async Task AddAsync_PersistsPackageSizeAndExtendedFields()
    {
        using SqliteTestDatabase database = new();
        var productId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            await repository.AddAsync(new Product(
                productId,
                "Nutella",
                new MacroNutrients(539m, 6.3m, 30.9m, 57.5m),
                "3017624010701",
                packageSizeGrams: 400m,
                details: new NutritionFacts(saturatedFat: 10.6m, sugars: 56.3m, vitaminD: 0.000005m)));
        }

        Product? stored;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            stored = await repository.GetByIdAsync(productId);
        }

        Assert.NotNull(stored);
        Assert.Equal(400m, stored.PackageSizeGrams);
        Assert.Equal(10.6m, stored.Details.SaturatedFat);
        Assert.Equal(56.3m, stored.Details.Sugars);
        Assert.Equal(0.000005m, stored.Details.VitaminD);
        Assert.Null(stored.Details.Fiber);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChangedPackageSizeAndExtendedFields()
    {
        using SqliteTestDatabase database = new();
        var productId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            await repository.AddAsync(new Product(
                productId,
                "Nutella",
                new MacroNutrients(539m, 6.3m, 30.9m, 57.5m),
                "3017624010701",
                packageSizeGrams: 400m,
                details: new NutritionFacts(saturatedFat: 10.6m, sugars: 56.3m)));
        }

        Result result;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            result = await repository.UpdateAsync(new Product(
                productId,
                "Nutella",
                new MacroNutrients(539m, 6.3m, 30.9m, 57.5m),
                "3017624010701",
                packageSizeGrams: 350m,
                details: new NutritionFacts(saturatedFat: 11m, sugars: 50m, fiber: 2m)));
        }

        Product? stored;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            stored = await repository.GetByIdAsync(productId);
        }

        Assert.True(result.IsSuccess);
        Assert.NotNull(stored);
        Assert.Equal(350m, stored.PackageSizeGrams);
        Assert.Equal(11m, stored.Details.SaturatedFat);
        Assert.Equal(50m, stored.Details.Sugars);
        Assert.Equal(2m, stored.Details.Fiber);
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
    public async Task DeleteAsync_RemovesProduct_EvenWhenUsedByRecipe()
    {
        using SqliteTestDatabase database = new();
        var productId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository products = new(context, new TestCurrentUser(OwnerId));
            await products.AddAsync(NewProduct(productId, "Oats"));

            Recipe recipe = new(Guid.NewGuid(), "Porridge", 2);
            recipe.AddIngredient(new RecipeIngredient(
                productId,
                "Oats",
                new MacroNutrients(100m, 10m, 5m, 20m),
                new GramAmount(150m)));
            RecipeRepository recipes = new(context, new TestCurrentUser(OwnerId));
            await recipes.AddAsync(recipe);
        }

        Result result;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository products = new(context, new TestCurrentUser(OwnerId));
            result = await products.DeleteAsync(productId);
        }

        Product? remaining;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository products = new(context, new TestCurrentUser(OwnerId));
            remaining = await products.GetByIdAsync(productId);
        }

        // "Keep historical": deletion succeeds even though a recipe references the product.
        Assert.True(result.IsSuccess);
        Assert.Null(remaining);
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

    [Fact]
    public async Task AddAsync_PersistsBrandAndCategory()
    {
        using SqliteTestDatabase database = new();
        var productId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            await repository.AddAsync(new Product(
                productId,
                "Nutella",
                new MacroNutrients(539m, 6.3m, 30.9m, 57.5m),
                brand: "Ferrero",
                category: ProductCategory.PantryAndDryGoods));
        }

        Product? stored;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            stored = await repository.GetByIdAsync(productId);
        }

        Assert.NotNull(stored);
        Assert.Equal("Ferrero", stored.Brand);
        Assert.Equal(ProductCategory.PantryAndDryGoods, stored.Category);
    }

    [Fact]
    public async Task UpdateAsync_ChangesBrandAndCategory_IncludingClearingToNull()
    {
        using SqliteTestDatabase database = new();
        var productId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            await repository.AddAsync(new Product(
                productId,
                "Yogurt",
                new MacroNutrients(60m, 4m, 3m, 5m),
                brand: "Zott",
                category: ProductCategory.Dairy));
        }

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            await repository.UpdateAsync(new Product(
                productId,
                "Yogurt",
                new MacroNutrients(60m, 4m, 3m, 5m),
                brand: null,
                category: null));
        }

        Product? stored;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            stored = await repository.GetByIdAsync(productId);
        }

        Assert.NotNull(stored);
        Assert.Null(stored.Brand);
        Assert.Null(stored.Category);
    }

    [Fact]
    public async Task GetCatalogAsync_ReturnsOnlyOwnersProducts_WithTotal()
    {
        using SqliteTestDatabase database = new();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository owner = new(context, new TestCurrentUser(OwnerId));
            await owner.AddAsync(Categorized(Guid.NewGuid(), "Oats", ProductCategory.GrainsAndBread));
            await owner.AddAsync(Categorized(Guid.NewGuid(), "Rice", ProductCategory.GrainsAndBread));

            ProductRepository other = new(context, new TestCurrentUser(OtherId));
            await other.AddAsync(Categorized(Guid.NewGuid(), "Beans", ProductCategory.Vegetables));
        }

        ProductCatalogResult page;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            page = await repository.GetCatalogAsync(
                search: null,
                category: null,
                uncategorizedOnly: false,
                sort: ProductCatalogSort.NameAsc,
                skip: 0,
                take: 50);
        }

        Assert.Equal(2, page.Total);
        Assert.All(page.Items, product => Assert.Contains(product.Name, new[] { "Oats", "Rice" }));
    }

    [Fact]
    public async Task GetCatalogAsync_FiltersBySpecificCategory()
    {
        using SqliteTestDatabase database = new();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            await repository.AddAsync(Categorized(Guid.NewGuid(), "Apple", ProductCategory.Fruits));
            await repository.AddAsync(Categorized(Guid.NewGuid(), "Cucumber", ProductCategory.Vegetables));
            await repository.AddAsync(Categorized(Guid.NewGuid(), "Mystery", category: null));
        }

        ProductCatalogResult page;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            page = await repository.GetCatalogAsync(
                search: null,
                category: ProductCategory.Fruits,
                uncategorizedOnly: false,
                sort: ProductCatalogSort.NameAsc,
                skip: 0,
                take: 50);
        }

        Assert.Equal(1, page.Total);
        Product only = Assert.Single(page.Items);
        Assert.Equal("Apple", only.Name);
    }

    [Fact]
    public async Task GetCatalogAsync_FiltersUncategorizedOnly()
    {
        using SqliteTestDatabase database = new();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            await repository.AddAsync(Categorized(Guid.NewGuid(), "Apple", ProductCategory.Fruits));
            await repository.AddAsync(Categorized(Guid.NewGuid(), "Mystery", category: null));
        }

        ProductCatalogResult page;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            page = await repository.GetCatalogAsync(
                search: null,
                category: null,
                uncategorizedOnly: true,
                sort: ProductCatalogSort.NameAsc,
                skip: 0,
                take: 50);
        }

        Product only = Assert.Single(page.Items);
        Assert.Equal("Mystery", only.Name);
        Assert.Null(only.Category);
    }

    [Fact]
    public async Task GetCatalogAsync_SearchesNameAndBarcode()
    {
        using SqliteTestDatabase database = new();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            await repository.AddAsync(
                Categorized(Guid.NewGuid(), "Almond milk", ProductCategory.Beverages, barcode: "11110000"));
            await repository.AddAsync(
                Categorized(Guid.NewGuid(), "Cow milk", ProductCategory.Dairy, barcode: "22220000"));
            await repository.AddAsync(
                Categorized(Guid.NewGuid(), "Oat flakes", ProductCategory.GrainsAndBread, barcode: "33330000"));
        }

        ProductCatalogResult byName;
        ProductCatalogResult byBarcode;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            byName = await repository.GetCatalogAsync(
                "milk", null, false, ProductCatalogSort.NameAsc, 0, 50);
            byBarcode = await repository.GetCatalogAsync(
                "3333", null, false, ProductCatalogSort.NameAsc, 0, 50);
        }

        Assert.Equal(2, byName.Total);
        Product barcodeHit = Assert.Single(byBarcode.Items);
        Assert.Equal("Oat flakes", barcodeHit.Name);
    }

    [Fact]
    public async Task GetCatalogAsync_SortsByCaloriesAscending()
    {
        using SqliteTestDatabase database = new();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            await repository.AddAsync(Categorized(Guid.NewGuid(), "Oil", ProductCategory.PantryAndDryGoods, calories: 900m));
            await repository.AddAsync(Categorized(Guid.NewGuid(), "Lettuce", ProductCategory.Vegetables, calories: 15m));
            await repository.AddAsync(Categorized(Guid.NewGuid(), "Bread", ProductCategory.GrainsAndBread, calories: 250m));
        }

        ProductCatalogResult page;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            page = await repository.GetCatalogAsync(
                null, null, false, ProductCatalogSort.CaloriesAsc, 0, 50);
        }

        Assert.Equal(
            new[] { "Lettuce", "Bread", "Oil" },
            page.Items.Select(product => product.Name).ToArray());
    }

    [Fact]
    public async Task GetCatalogAsync_PaginatesWithStableOrder()
    {
        using SqliteTestDatabase database = new();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            await repository.AddAsync(Categorized(Guid.NewGuid(), "Apple", ProductCategory.Fruits));
            await repository.AddAsync(Categorized(Guid.NewGuid(), "Banana", ProductCategory.Fruits));
            await repository.AddAsync(Categorized(Guid.NewGuid(), "Cherry", ProductCategory.Fruits));
        }

        ProductCatalogResult firstPage;
        ProductCatalogResult secondPage;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ProductRepository repository = new(context, new TestCurrentUser(OwnerId));
            firstPage = await repository.GetCatalogAsync(null, null, false, ProductCatalogSort.NameAsc, 0, 2);
            secondPage = await repository.GetCatalogAsync(null, null, false, ProductCatalogSort.NameAsc, 2, 2);
        }

        Assert.Equal(3, firstPage.Total);
        Assert.Equal(new[] { "Apple", "Banana" }, firstPage.Items.Select(p => p.Name).ToArray());
        Product last = Assert.Single(secondPage.Items);
        Assert.Equal("Cherry", last.Name);
    }

    private static Product NewProduct(Guid id, string name)
    {
        return new Product(id, name, new MacroNutrients(100m, 10m, 5m, 20m));
    }

    private static Product Categorized(
        Guid id,
        string name,
        ProductCategory? category,
        decimal calories = 100m,
        string? barcode = null)
    {
        return new Product(
            id,
            name,
            new MacroNutrients(calories, 10m, 5m, 20m),
            barcode,
            category: category);
    }
}
