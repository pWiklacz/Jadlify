using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jadlify.API.Tests.Authentication;

public class AnonymousSurfaceTests
{
    [Fact]
    public async Task Health_ShouldBeReachableAnonymously()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        HttpResponseMessage response = await client.GetAsync("/health");

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public void RuntimeHttpEndpoints_ShouldNotAllowAnonymousAccessExceptHealth()
    {
        using WebApplicationFactory<Program> factory = new();
        _ = factory.CreateClient();

        RouteEndpoint[] endpoints = factory.Services
            .GetRequiredService<IEnumerable<EndpointDataSource>>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata.GetMetadata<HttpMethodMetadata>() is not null)
            .ToArray();

        Assert.Contains(endpoints, IsHealthEndpoint);

        RouteEndpoint[] anonymousNonHealthEndpoints = endpoints
            .Where(endpoint => !IsHealthEndpoint(endpoint))
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .ToArray();

        Assert.Empty(anonymousNonHealthEndpoints);
    }

    [Fact]
    public void FallbackAuthorizationPolicy_ShouldRequireAuthenticatedSubjectClaim()
    {
        using WebApplicationFactory<Program> factory = new();
        _ = factory.CreateClient();

        AuthorizationOptions options = factory.Services
            .GetRequiredService<IOptions<AuthorizationOptions>>()
            .Value;

        Assert.NotNull(options.FallbackPolicy);
        Assert.Contains(
            options.FallbackPolicy.Requirements.OfType<DenyAnonymousAuthorizationRequirement>(),
            requirement => requirement is not null);
        Assert.Contains(
            options.FallbackPolicy.Requirements.OfType<ClaimsAuthorizationRequirement>(),
            requirement => requirement.ClaimType == "sub");
    }

    private static bool IsHealthEndpoint(RouteEndpoint endpoint)
    {
        string route = endpoint.RoutePattern.RawText ?? string.Empty;

        return route.TrimStart('/').Equals("health", StringComparison.OrdinalIgnoreCase);
    }
}
