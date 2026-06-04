using System.Net;
using System.Net.Http.Json;
using Jadlify.API.Planning;
using Jadlify.API.Tests.Common;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Jadlify.API.Tests.Planning;

public class DailyGoalEndpointsTests
{
    private const string UserA = "user-a";
    private const string UserB = "user-b";

    [Fact]
    public async Task DailyGoalRequiresAuthentication()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        HttpResponseMessage response = await client.GetAsync("/api/daily-goal");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsNull_WhenNoGoalConfigured()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);

        HttpResponseMessage response = await client.GetAsync("/api/daily-goal");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        DailyGoalResponse? body = await response.Content.ReadFromJsonAsync<DailyGoalResponse>();
        Assert.Null(body);
    }

    [Fact]
    public async Task Upsert_ThenGet_ReturnsCurrentGoal()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);

        HttpResponseMessage put = await client.PutAsJsonAsync(
            "/api/daily-goal",
            new UpsertDailyGoalRequest(2000m, 150m, 70m, 200m));
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        DailyGoalResponse? goal = await client.GetFromJsonAsync<DailyGoalResponse>("/api/daily-goal");

        Assert.NotNull(goal);
        Assert.Equal(2000m, goal!.Calories);
        Assert.Equal(150m, goal.Protein);
        Assert.Equal(70m, goal.Fat);
        Assert.Equal(200m, goal.Carbohydrates);
    }

    [Fact]
    public async Task Upsert_ReplacesExistingGoal()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);

        await client.PutAsJsonAsync("/api/daily-goal", new UpsertDailyGoalRequest(2000m, 150m, 70m, 200m));
        await client.PutAsJsonAsync("/api/daily-goal", new UpsertDailyGoalRequest(1800m, 140m, 60m, 180m));

        DailyGoalResponse? goal = await client.GetFromJsonAsync<DailyGoalResponse>("/api/daily-goal");

        Assert.NotNull(goal);
        Assert.Equal(1800m, goal!.Calories);
        Assert.Equal(140m, goal.Protein);
    }

    [Fact]
    public async Task Upsert_RejectsNonPositiveCalories()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            "/api/daily-goal",
            new UpsertDailyGoalRequest(0m, 150m, 70m, 200m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UserB_CannotSeeUserAGoal()
    {
        using TestApiFactory factory = new();
        using HttpClient clientA = factory.CreateClientAs(UserA);
        using HttpClient clientB = factory.CreateClientAs(UserB);

        await clientA.PutAsJsonAsync("/api/daily-goal", new UpsertDailyGoalRequest(2000m, 150m, 70m, 200m));

        HttpResponseMessage response = await clientB.GetAsync("/api/daily-goal");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        DailyGoalResponse? body = await response.Content.ReadFromJsonAsync<DailyGoalResponse>();
        Assert.Null(body);
    }
}
