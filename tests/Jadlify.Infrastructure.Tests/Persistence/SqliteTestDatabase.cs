using Jadlify.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Jadlify.Infrastructure.Tests.Persistence;

/// <summary>
/// Hosts a throwaway relational database for persistence tests. SQLite in-memory keeps the
/// full relational mapping (owned types, foreign keys, decimal scale) under test without
/// Supabase secrets, Docker, or network access. The connection stays open for the lifetime
/// of the instance so the shared in-memory database survives across contexts.
/// </summary>
internal sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JadlifyDbContext> _options;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<JadlifyDbContext>()
            .UseSqlite(_connection)
            .Options;

        using JadlifyDbContext context = CreateContext();
        context.Database.EnsureCreated();
    }

    public JadlifyDbContext CreateContext()
    {
        return new JadlifyDbContext(_options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
