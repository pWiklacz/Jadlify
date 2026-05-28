using Microsoft.EntityFrameworkCore;

namespace Jadlify.Infrastructure.Persistence;

public sealed class JadlifyDbContext : DbContext
{
    public const string ConnectionStringName = "JadlifyDatabase";

    public JadlifyDbContext(DbContextOptions<JadlifyDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JadlifyDbContext).Assembly);
    }
}
