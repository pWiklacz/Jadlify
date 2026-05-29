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
    public void RecipeIngredient_RestrictsProductDeletionButCascadesWithRecipe()
    {
        using SqliteTestDatabase database = new();
        using JadlifyDbContext context = database.CreateContext();

        IEntityType ingredient = SingleEntityType<RecipeIngredient>(context);

        IForeignKey productForeignKey = ingredient.GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Product));
        Assert.Equal(DeleteBehavior.Restrict, productForeignKey.DeleteBehavior);

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
