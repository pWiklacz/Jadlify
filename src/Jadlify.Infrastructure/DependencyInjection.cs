using Jadlify.Application.Planning;
using Jadlify.Application.Products;
using Jadlify.Application.Recipes;
using Jadlify.Infrastructure.OpenFoodFacts;
using Jadlify.Infrastructure.Persistence;
using Jadlify.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        services.Configure<OpenFoodFactsOptions>(
            configuration.GetSection(OpenFoodFactsOptions.SectionName));
        services.AddHttpClient<IBarcodeProductLookup, OpenFoodFactsBarcodeLookup>(
            (serviceProvider, client) =>
            {
                OpenFoodFactsOptions options = serviceProvider
                    .GetRequiredService<IOptions<OpenFoodFactsOptions>>().Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                // OFF bans anonymous bot-like traffic; send a descriptive UA. Add it without
                // validation so a non-structured UA string never throws a FormatException.
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.UserAgent);
            });

        return services;
    }
}
