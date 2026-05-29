using Jadlify.Domain.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jadlify.Infrastructure.Persistence.Configurations;

internal sealed class DailyMacroGoalConfiguration : IEntityTypeConfiguration<DailyMacroGoal>
{
    public void Configure(EntityTypeBuilder<DailyMacroGoal> builder)
    {
        builder.ToTable("daily_macro_goals");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("id");

        builder.Property<string>(PersistenceConstants.UserIdProperty)
            .HasColumnName(PersistenceConstants.UserIdColumn)
            .IsRequired();
        builder.HasIndex(PersistenceConstants.UserIdProperty).IsUnique();

        builder.OwnsOne(g => g.Target, macro =>
        {
            macro.Property(m => m.Calories).HasColumnName("target_calories").HasPrecision(10, 2);
            macro.Property(m => m.Protein).HasColumnName("target_protein").HasPrecision(10, 2);
            macro.Property(m => m.Fat).HasColumnName("target_fat").HasPrecision(10, 2);
            macro.Property(m => m.Carbohydrates).HasColumnName("target_carbohydrates").HasPrecision(10, 2);
        });
        builder.Navigation(g => g.Target).IsRequired();
    }
}
