using Jadlify.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jadlify.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.Name).HasColumnName("name").IsRequired();
        builder.Property(p => p.Barcode).HasColumnName("barcode");

        builder.Property<string>(PersistenceConstants.UserIdProperty)
            .HasColumnName(PersistenceConstants.UserIdColumn)
            .IsRequired();
        builder.HasIndex(PersistenceConstants.UserIdProperty);
        builder.HasIndex(PersistenceConstants.UserIdProperty, nameof(Product.Barcode));

        builder.OwnsOne(p => p.Per100Grams, macro =>
        {
            macro.Property(m => m.Calories).HasColumnName("calories_per_100g").HasPrecision(10, 2);
            macro.Property(m => m.Protein).HasColumnName("protein_per_100g").HasPrecision(10, 2);
            macro.Property(m => m.Fat).HasColumnName("fat_per_100g").HasPrecision(10, 2);
            macro.Property(m => m.Carbohydrates).HasColumnName("carbohydrates_per_100g").HasPrecision(10, 2);
        });
        builder.Navigation(p => p.Per100Grams).IsRequired();

        builder.Property(p => p.PackageSizeGrams)
            .HasColumnName("package_size_grams")
            .HasPrecision(10, 2);

        // Extended per-100g profile. All columns are nullable (OFF coverage is sparse), and
        // each uses (12,6) precision because OFF normalizes vitamins/minerals to grams — the
        // values are sub-milligram (e.g. 0.0006 g) and (10,2) would truncate them to zero.
        builder.OwnsOne(p => p.Details, details =>
        {
            details.Property(d => d.SaturatedFat).HasColumnName("saturated_fat_per_100g").HasPrecision(12, 6);
            details.Property(d => d.MonounsaturatedFat).HasColumnName("monounsaturated_fat_per_100g").HasPrecision(12, 6);
            details.Property(d => d.PolyunsaturatedFat).HasColumnName("polyunsaturated_fat_per_100g").HasPrecision(12, 6);
            details.Property(d => d.TransFat).HasColumnName("trans_fat_per_100g").HasPrecision(12, 6);
            details.Property(d => d.Sugars).HasColumnName("sugars_per_100g").HasPrecision(12, 6);
            details.Property(d => d.Fiber).HasColumnName("fiber_per_100g").HasPrecision(12, 6);
            details.Property(d => d.Salt).HasColumnName("salt_per_100g").HasPrecision(12, 6);
            details.Property(d => d.Sodium).HasColumnName("sodium_per_100g").HasPrecision(12, 6);
            details.Property(d => d.Potassium).HasColumnName("potassium_per_100g").HasPrecision(12, 6);
            details.Property(d => d.Calcium).HasColumnName("calcium_per_100g").HasPrecision(12, 6);
            details.Property(d => d.Iron).HasColumnName("iron_per_100g").HasPrecision(12, 6);
            details.Property(d => d.VitaminA).HasColumnName("vitamin_a_per_100g").HasPrecision(12, 6);
            details.Property(d => d.VitaminC).HasColumnName("vitamin_c_per_100g").HasPrecision(12, 6);
            details.Property(d => d.VitaminD).HasColumnName("vitamin_d_per_100g").HasPrecision(12, 6);
        });
        builder.Navigation(p => p.Details).IsRequired();
    }
}
