using Jadlify.Domain.Planning;
using Jadlify.Domain.Products;
using Jadlify.Domain.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Jadlify.Infrastructure.Persistence;

public sealed class JadlifyDbContext : DbContext
{
    public const string ConnectionStringName = "JadlifyDatabase";

    public JadlifyDbContext(DbContextOptions<JadlifyDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Recipe> Recipes => Set<Recipe>();

    public DbSet<DailyMacroGoal> DailyMacroGoals => Set<DailyMacroGoal>();

    public DbSet<MealPlanEntry> MealPlanEntries => Set<MealPlanEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JadlifyDbContext).Assembly);
    }
}
