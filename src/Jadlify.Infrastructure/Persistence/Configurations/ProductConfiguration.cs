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
    }
}
