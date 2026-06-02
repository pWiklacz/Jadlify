using System.Net.Http.Headers;
using Jadlify.Application.Products;
using Jadlify.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jadlify.API.Tests.Common;

/// <summary>
/// Spins up the real API pipeline for integration tests with two production dependencies
/// swapped out: the Npgsql <see cref="JadlifyDbContext"/> is replaced by a shared in-memory
/// SQLite connection (full relational mapping, no Supabase secrets or network), and
/// <see cref="IBarcodeProductLookup"/> is replaced by a controllable <see cref="StubBarcodeProductLookup"/>.
/// Auth runs through the deterministic <see cref="TestAuthenticationHandler"/> (token == 'sub').
/// Each instance owns its own SQLite database, so tests are isolated.
/// </summary>
internal sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public TestApiFactory()
    {
        // Keep the connection open for the factory's lifetime so the in-memory database
        // survives across request scopes and DbContext instances.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    /// <summary>The lookup stub backing <see cref="IBarcodeProductLookup"/>; configure before issuing a request.</summary>
    public StubBarcodeProductLookup BarcodeLookup { get; } = new();

    /// <summary>Creates an HTTPS client authenticated as <paramref name="subject"/> (the bearer token is the 'sub' claim).</summary>
    public HttpClient CreateClientAs(string subject)
    {
        HttpClient client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", subject);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseWebRoot(AppContext.BaseDirectory);
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services => services.RemoveAll<ILoggerProvider>());
        builder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(TestAuthenticationHandler.AuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.AuthenticationScheme,
                    _ => { });

            // Drop every DbContextOptions / provider-configuration descriptor before re-adding,
            // otherwise EF Core applies the leftover Npgsql configuration alongside SQLite and
            // throws "only a single database provider can be registered".
            var dbDescriptors = services
                .Where(descriptor =>
                    descriptor.ServiceType.FullName is { } name &&
                    (name.Contains("DbContextOptions", StringComparison.Ordinal) ||
                     name.Contains("IDbContextOptionsConfiguration", StringComparison.Ordinal)))
                .ToList();
            foreach (ServiceDescriptor descriptor in dbDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<JadlifyDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<IBarcodeProductLookup>();
            services.AddSingleton<IBarcodeProductLookup>(BarcodeLookup);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        IHost host = base.CreateHost(builder);

        using IServiceScope scope = host.Services.CreateScope();
        JadlifyDbContext context = scope.ServiceProvider.GetRequiredService<JadlifyDbContext>();
        context.Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
