using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Products;

namespace Jadlify.Domain.Tests.Products;

public class ProductTests
{
    private static readonly MacroNutrients Macros = new(100m, 10m, 5m, 20m);

    [Fact]
    public void Constructor_StoresBrandAndCategory()
    {
        Product product = new(
            Guid.NewGuid(),
            "Nutella",
            Macros,
            brand: "Ferrero",
            category: ProductCategory.PantryAndDryGoods);

        Assert.Equal("Ferrero", product.Brand);
        Assert.Equal(ProductCategory.PantryAndDryGoods, product.Category);
    }

    [Fact]
    public void Constructor_DefaultsBrandAndCategoryToNull_WhenOmitted()
    {
        // Backward compatible with pre-brand/category call sites.
        Product product = new(Guid.NewGuid(), "Oats", Macros);

        Assert.Null(product.Brand);
        Assert.Null(product.Category);
    }

    [Fact]
    public void Constructor_AllowsNullCategory_AsValidUncategorizedState()
    {
        Product product = new(Guid.NewGuid(), "Oats", Macros, brand: "Bio Planet", category: null);

        Assert.Equal("Bio Planet", product.Brand);
        Assert.Null(product.Category);
    }

    [Fact]
    public void ProductCategory_DefinesTheNineCanonicalBuckets_InShoppingOrder()
    {
        // The catalog "by category" sort and the UI label map both depend on this set and order.
        ProductCategory[] expected =
        [
            ProductCategory.Vegetables,
            ProductCategory.Fruits,
            ProductCategory.MeatAndFish,
            ProductCategory.Dairy,
            ProductCategory.GrainsAndBread,
            ProductCategory.PantryAndDryGoods,
            ProductCategory.Frozen,
            ProductCategory.Beverages,
            ProductCategory.Other,
        ];

        Assert.Equal(expected, Enum.GetValues<ProductCategory>());
    }
}
