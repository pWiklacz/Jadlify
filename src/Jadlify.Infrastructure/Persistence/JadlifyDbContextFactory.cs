using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jadlify.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used only by EF Core tooling to build the model and emit
/// migrations. It uses a local non-secret placeholder; real Supabase Postgres
/// credentials are supplied at runtime through configuration.
/// </summary>
internal sealed class JadlifyDbContextFactory : IDesignTimeDbContextFactory<JadlifyDbContext>
{
    private const string DesignTimeConnectionString =
        "Host=localhost;Port=5432;Database=jadlify_design;Username=postgres";

    public JadlifyDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<JadlifyDbContext> options =
            new DbContextOptionsBuilder<JadlifyDbContext>()
                .UseNpgsql(DesignTimeConnectionString)
                .Options;

        return new JadlifyDbContext(options);
    }
}
