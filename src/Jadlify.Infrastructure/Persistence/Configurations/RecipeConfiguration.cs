using Jadlify.Domain.Products;
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
            ingredient.HasKey("RecipeId", nameof(RecipeIngredient.ProductId));

            ingredient.OwnsOne(i => i.WholeRecipeAmount, grams =>
            {
                grams.Property(g => g.Value).HasColumnName("whole_recipe_grams").HasPrecision(10, 2);
            });
            ingredient.Navigation(i => i.WholeRecipeAmount).IsRequired();

            ingredient.HasOne<Product>()
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
