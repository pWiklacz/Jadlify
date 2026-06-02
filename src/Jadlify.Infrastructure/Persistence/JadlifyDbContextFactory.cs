using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jadlify.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used only by EF Core tooling to build the model and emit
/// migrations. It resolves the real database credentials from local user secrets or
/// environment variables, falling back to a non-secret placeholder.
/// </summary>
internal sealed class JadlifyDbContextFactory : IDesignTimeDbContextFactory<JadlifyDbContext>
{
    private const string DesignTimeConnectionString =
        "Host=localhost;Port=5432;Database=jadlify_design;Username=postgres";

    private const string UserSecretsId = "000e6781-a9d8-4572-9cfd-4024480ae371";

    public JadlifyDbContext CreateDbContext(string[] args)
    {
        string connectionString = GetConnectionString();

        DbContextOptions<JadlifyDbContext> options = new DbContextOptionsBuilder<JadlifyDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new JadlifyDbContext(options);
    }

    private static string GetConnectionString()
    {
        // 1. Try to get connection string from environment variables
        string? connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__JadlifyDatabase")
            ?? Environment.GetEnvironmentVariable("JADLIFY_CONNECTION_STRING");

        if (!string.IsNullOrEmpty(connectionString))
        {
            return connectionString;
        }

        // 2. Try to load connection string from local user secrets (secrets.json)
        try
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string secretsPath = Path.Combine(appData, "Microsoft", "UserSecrets", UserSecretsId, "secrets.json");

            if (File.Exists(secretsPath))
            {
                string json = File.ReadAllText(secretsPath);
                using var doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                // dotnet user-secrets saves a flat JSON with colon separators
                if (root.TryGetProperty("ConnectionStrings:JadlifyDatabase", out JsonElement val))
                {
                    string? s = val.GetString();
                    if (!string.IsNullOrEmpty(s))
                    {
                        return s;
                    }
                }
            }
        }
        catch
        {
            // Fallback on failure
        }

        return DesignTimeConnectionString;
    }
}
