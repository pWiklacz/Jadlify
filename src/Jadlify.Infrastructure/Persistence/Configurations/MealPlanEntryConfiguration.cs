using Jadlify.Domain.Planning;
using Jadlify.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jadlify.Infrastructure.Persistence.Configurations;

internal sealed class MealPlanEntryConfiguration : IEntityTypeConfiguration<MealPlanEntry>
{
    /// <summary>
    /// Enforces the domain's exactly-one-source rule in the database itself, so no code path —
    /// migration, batch operation, or manual fix — can leave a row that is both or neither.
    /// The quantity's unit follows the discriminator, so a positive quantity is required for
    /// either variant.
    /// </summary>
    private const string SourceCheckConstraint = """
        quantity > 0 AND (
            (source = 'Recipe' AND recipe_id IS NOT NULL AND product_id IS NULL)
            OR (source = 'Product' AND recipe_id IS NULL AND product_id IS NOT NULL)
        )
        """;

    public void Configure(EntityTypeBuilder<MealPlanEntry> builder)
    {
        builder.ToTable(
            "meal_plan_entries",
            table => table.HasCheckConstraint("ck_meal_plan_entries_exactly_one_source", SourceCheckConstraint));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Date).HasColumnName("date");
        builder.Property(e => e.MealType)
            .HasColumnName("meal_type")
            .HasConversion<string>()
            .HasMaxLength(20);

        // Discriminator stored by its stable enum name, matching the wire and the check
        // constraint above; never the ordinal, so member reordering cannot corrupt rows.
        builder.Property(e => e.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // One quantity column for both variants: portions (half-steps) or grams. Scale 3 covers
        // a 0.5 portion and a fractional gram amount without widening either variant's range.
        builder.Property(e => e.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(12, 3);

        builder.Property(e => e.RecipeId).HasColumnName("recipe_id");

        builder.Ignore(e => e.RecipePortions);
        builder.Ignore(e => e.ProductGrams);

        builder.Property<string>(PersistenceConstants.UserIdProperty)
            .HasColumnName(PersistenceConstants.UserIdColumn)
            .IsRequired();
        builder.HasIndex(PersistenceConstants.UserIdProperty, nameof(MealPlanEntry.Date));

        // Recipe entries keep the Restrict FK: deleting a recipe that is still planned stays a
        // conflict rather than silently emptying a past day. The FK is now optional because a
        // product entry has no recipe at all.
        builder.HasOne<Recipe>()
            .WithMany()
            .HasForeignKey(e => e.RecipeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // The product snapshot is owned and table-shared: all columns are null for a recipe
        // entry. product_id is deliberately not a foreign key — the snapshot must survive the
        // catalog product being edited or deleted, exactly as recipe ingredients do.
        builder.OwnsOne(e => e.Product, product =>
        {
            product.Property(p => p.ProductId).HasColumnName("product_id");
            product.Property(p => p.Name).HasColumnName("product_name").HasMaxLength(200);
            product.Property(p => p.Category)
                .HasColumnName("product_category")
                .HasConversion<string>()
                .HasMaxLength(40);

            product.OwnsOne(p => p.Per100Grams, macro =>
            {
                macro.Property(m => m.Calories).HasColumnName("product_calories_per_100g").HasPrecision(10, 2);
                macro.Property(m => m.Protein).HasColumnName("product_protein_per_100g").HasPrecision(10, 2);
                macro.Property(m => m.Fat).HasColumnName("product_fat_per_100g").HasPrecision(10, 2);
                macro.Property(m => m.Carbohydrates)
                    .HasColumnName("product_carbohydrates_per_100g")
                    .HasPrecision(10, 2);
            });
            product.Navigation(p => p.Per100Grams).IsRequired();

            product.HasIndex(p => p.ProductId);
        });
        builder.Navigation(e => e.Product).IsRequired(false);
    }
}
