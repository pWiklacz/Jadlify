using Jadlify.Infrastructure.Persistence;
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

        return services;
    }
}
