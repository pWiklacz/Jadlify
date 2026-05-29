using Jadlify.Domain.Planning;
using Jadlify.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jadlify.Infrastructure.Persistence.Configurations;

internal sealed class MealPlanEntryConfiguration : IEntityTypeConfiguration<MealPlanEntry>
{
    public void Configure(EntityTypeBuilder<MealPlanEntry> builder)
    {
        builder.ToTable("meal_plan_entries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Date).HasColumnName("date");
        builder.Property(e => e.RecipeId).HasColumnName("recipe_id");
        builder.Property(e => e.MealType)
            .HasColumnName("meal_type")
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(e => e.Portions).HasColumnName("portions");

        builder.Property<string>(PersistenceConstants.UserIdProperty)
            .HasColumnName(PersistenceConstants.UserIdColumn)
            .IsRequired();
        builder.HasIndex(PersistenceConstants.UserIdProperty, nameof(MealPlanEntry.Date));

        builder.HasOne<Recipe>()
            .WithMany()
            .HasForeignKey(e => e.RecipeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
