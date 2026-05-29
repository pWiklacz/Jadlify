using Jadlify.Application.Planning;
using Jadlify.Application.Products;
using Jadlify.Application.Recipes;
using Jadlify.Infrastructure.Persistence;
using Jadlify.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jadlify.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString(JadlifyDbContext.ConnectionStringName);

        services.AddDbContext<JadlifyDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IDailyMacroGoalRepository, DailyMacroGoalRepository>();
        services.AddScoped<IMealPlanRepository, MealPlanRepository>();

        return services;
    }
}
