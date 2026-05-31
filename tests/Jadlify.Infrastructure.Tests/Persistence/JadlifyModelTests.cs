using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Products;
using Jadlify.Domain.Recipes;
using Jadlify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Jadlify.Infrastructure.Tests.Persistence;

public class JadlifyModelTests
{
    [Theory]
    [InlineData(typeof(Product))]
    [InlineData(typeof(Recipe))]
    [InlineData(typeof(DailyMacroGoal))]
    [InlineData(typeof(MealPlanEntry))]
    public void UserOwnedEntities_MapRequiredUserIdShadowColumn(Type entityClrType)
    {
        using SqliteTestDatabase database = new();
        using JadlifyDbContext context = database.CreateContext();

        IEntityType entityType = context.Model.FindEntityType(entityClrType)!;
        IProperty? userId = entityType.FindProperty(PersistenceUserIdProperty);

        Assert.NotNull(userId);
        Assert.True(userId.IsShadowProperty());
        Assert.False(userId.IsNullable);

        StoreObjectIdentifier table = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)!.Value;
        Assert.Equal("user_id", userId.GetColumnName(table));
    }

    [Fact]
    public void ProductMacros_UseTwoDecimalScale()
    {
        AssertMacroPrecision<Product>(nameof(Product.Per100Grams));
    }

    [Fact]
    public void ProductExtendedNutrition_UsesHighPrecisionScale()
    {
        using SqliteTestDatabase database = new();
        using JadlifyDbContext context = database.CreateContext();

        IEntityType product = context.Model.FindEntityType(typeof(Product))!;
        IEntityType details = product.FindNavigation(nameof(Product.Details))!.TargetEntityType;

        string[] components =
        [
            nameof(NutritionFacts.SaturatedFat),
            nameof(NutritionFacts.MonounsaturatedFat),
            nameof(NutritionFacts.PolyunsaturatedFat),
            nameof(NutritionFacts.TransFat),
            nameof(NutritionFacts.Sugars),
            nameof(NutritionFacts.Fiber),
            nameof(NutritionFacts.Salt),
            nameof(NutritionFacts.Sodium),
            nameof(NutritionFacts.Potassium),
            nameof(NutritionFacts.Calcium),
            nameof(NutritionFacts.Iron),
            nameof(NutritionFacts.VitaminA),
            nameof(NutritionFacts.VitaminC),
            nameof(NutritionFacts.VitaminD),
        ];

        foreach (string component in components)
        {
            IProperty mapped = details.FindProperty(component)!;
            // (12,6) so OFF's gram-normalized sub-milligram micros are not truncated to zero.
            Assert.Equal(12, mapped.GetPrecision()!.Value);
            Assert.Equal(6, mapped.GetScale()!.Value);
            Assert.True(mapped.IsNullable);
        }
    }

    [Fact]
    public void ProductPackageSize_UsesTwoDecimalScale_AndIsNullable()
    {
        using SqliteTestDatabase database = new();
        using JadlifyDbContext context = database.CreateContext();

        IEntityType product = context.Model.FindEntityType(typeof(Product))!;
        IProperty packageSize = product.FindProperty(nameof(Product.PackageSizeGrams))!;

        Assert.Equal(10, packageSize.GetPrecision()!.Value);
        Assert.Equal(2, packageSize.GetScale()!.Value);
        Assert.True(packageSize.IsNullable);
    }

    [Fact]
    public void DailyMacroGoalTarget_UsesTwoDecimalScale()
    {
        AssertMacroPrecision<DailyMacroGoal>(nameof(DailyMacroGoal.Target));
    }

    [Fact]
    public void RecipeIngredientAmount_UsesTwoDecimalScale()
    {
        using SqliteTestDatabase database = new();
        using JadlifyDbContext context = database.CreateContext();

        IEntityType ingredient = SingleEntityType<RecipeIngredient>(context);
        IEntityType amount = ingredient.FindNavigation(nameof(RecipeIngredient.WholeRecipeAmount))!.TargetEntityType;
        IProperty value = amount.FindProperty(nameof(GramAmount.Value))!;

        Assert.Equal(10, value.GetPrecision()!.Value);
        Assert.Equal(2, value.GetScale()!.Value);
    }

    [Fact]
    public void RecipeIngredient_HasNoProductForeignKey_ButCascadesWithRecipe()
    {
        using SqliteTestDatabase database = new();
        using JadlifyDbContext context = database.CreateContext();

        IEntityType ingredient = SingleEntityType<RecipeIngredient>(context);

        // "Keep historical" (S-02): no FK to products, so a product can be deleted while the
        // recipe-ingredient row survives carrying its (now unenforced) product_id.
        Assert.DoesNotContain(
            ingredient.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Product));

        IForeignKey ownershipForeignKey = ingredient.GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Recipe));
        Assert.Equal(DeleteBehavior.Cascade, ownershipForeignKey.DeleteBehavior);
    }

    [Fact]
    public void MealPlanEntry_RestrictsRecipeDeletion()
    {
        using SqliteTestDatabase database = new();
        using JadlifyDbContext context = database.CreateContext();

        IEntityType entry = context.Model.FindEntityType(typeof(MealPlanEntry))!;
        IForeignKey recipeForeignKey = entry.GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Recipe));

        Assert.Equal(DeleteBehavior.Restrict, recipeForeignKey.DeleteBehavior);
    }

    private const string PersistenceUserIdProperty = "UserId";

    private static void AssertMacroPrecision<TOwner>(string navigationName)
        where TOwner : class
    {
        using SqliteTestDatabase database = new();
        using JadlifyDbContext context = database.CreateContext();

        IEntityType owner = context.Model.FindEntityType(typeof(TOwner))!;
        IEntityType macros = owner.FindNavigation(navigationName)!.TargetEntityType;

        string[] components =
        [
            nameof(MacroNutrients.Calories),
            nameof(MacroNutrients.Protein),
            nameof(MacroNutrients.Fat),
            nameof(MacroNutrients.Carbohydrates),
        ];

        foreach (string component in components)
        {
            IProperty mapped = macros.FindProperty(component)!;
            Assert.Equal(10, mapped.GetPrecision()!.Value);
            Assert.Equal(2, mapped.GetScale()!.Value);
        }
    }

    private static IEntityType SingleEntityType<TEntity>(JadlifyDbContext context)
    {
        return context.Model.GetEntityTypes().Single(entity => entity.ClrType == typeof(TEntity));
    }
}
