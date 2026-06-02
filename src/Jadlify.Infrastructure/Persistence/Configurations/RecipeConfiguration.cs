using Jadlify.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jadlify.Infrastructure.Persistence.Configurations;

internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("recipes");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.Name).HasColumnName("name").IsRequired();
        builder.Property(r => r.Portions).HasColumnName("portions");

        builder.Property<string>(PersistenceConstants.UserIdProperty)
            .HasColumnName(PersistenceConstants.UserIdColumn)
            .IsRequired();
        builder.HasIndex(PersistenceConstants.UserIdProperty);

        builder.OwnsMany(r => r.Ingredients, ingredient =>
        {
            ingredient.ToTable("recipe_ingredients");

            ingredient.WithOwner().HasForeignKey("RecipeId");
            ingredient.Property<Guid>("RecipeId").HasColumnName("recipe_id");
            ingredient.Property(i => i.ProductId).HasColumnName("product_id");
            ingredient.Property(i => i.ProductName).HasColumnName("product_name").IsRequired();
            ingredient.HasKey("RecipeId", nameof(RecipeIngredient.ProductId));

            ingredient.OwnsOne(i => i.Per100Grams, macro =>
            {
                macro.Property(m => m.Calories).HasColumnName("calories_per_100g").HasPrecision(10, 2);
                macro.Property(m => m.Protein).HasColumnName("protein_per_100g").HasPrecision(10, 2);
                macro.Property(m => m.Fat).HasColumnName("fat_per_100g").HasPrecision(10, 2);
                macro.Property(m => m.Carbohydrates).HasColumnName("carbohydrates_per_100g").HasPrecision(10, 2);
            });
            ingredient.Navigation(i => i.Per100Grams).IsRequired();

            ingredient.OwnsOne(i => i.WholeRecipeAmount, grams =>
            {
                grams.Property(g => g.Value).HasColumnName("whole_recipe_grams").HasPrecision(10, 2);
            });
            ingredient.Navigation(i => i.WholeRecipeAmount).IsRequired();

            // No FK to products: the "keep historical" delete policy (S-02) requires a product
            // to be deletable while the recipe-ingredient row survives carrying its product_id.
            // S-03 snapshots the product's name + per-100g macros into the ingredient so a recipe
            // keeps correct totals after its product is gone (cross-slice contract).
            ingredient.HasIndex(i => i.ProductId);
        });
    }
}
