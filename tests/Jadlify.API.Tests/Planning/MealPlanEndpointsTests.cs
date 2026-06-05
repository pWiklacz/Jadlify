using System.Net;
using System.Net.Http.Json;
using Jadlify.API.Planning;
using Jadlify.API.Products;
using Jadlify.API.Recipes;
using Jadlify.API.Tests.Common;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Jadlify.API.Tests.Planning;

public class MealPlanEndpointsTests
{
    private const string UserA = "user-a";
    private const string UserB = "user-b";
    private static readonly DateOnly Date = new(2026, 6, 1);
    private static readonly DateOnly OtherDate = new(2026, 6, 2);

    [Fact]
    public async Task MealPlanRequiresAuthentication()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        HttpResponseMessage response = await client.GetAsync($"/api/meal-plan?date={Iso(Date)}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SummaryRequiresAuthentication()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        HttpResponseMessage response = await client.GetAsync($"/api/meal-plan/summary?date={Iso(Date)}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsCreated_WithLocation()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/meal-plan",
            new AddMealPlanEntryRequest(Date, recipeId, "Breakfast", 1));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        CreatedMealPlanEntryResponse? body =
            await response.Content.ReadFromJsonAsync<CreatedMealPlanEntryResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
        Assert.Equal($"/api/meal-plan/{body.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task CreateThenList_ReturnsEntryWithRecipeName()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");
        await CreateEntryAsync(client, Date, recipeId, "Breakfast", 2);

        MealPlanEntryResponse[] entries = await ListAsync(client, Date);

        MealPlanEntryResponse entry = Assert.Single(entries);
        Assert.Equal(recipeId, entry.RecipeId);
        Assert.Equal("Porridge", entry.RecipeName);
        Assert.Equal("Breakfast", entry.MealType);
        Assert.Equal(2, entry.Portions);
        Assert.Equal(Date, entry.Date);
    }

    [Fact]
    public async Task List_FiltersBySelectedDate()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");
        await CreateEntryAsync(client, Date, recipeId, "Breakfast", 1);
        await CreateEntryAsync(client, OtherDate, recipeId, "Lunch", 1);

        MealPlanEntryResponse[] entries = await ListAsync(client, Date);

        MealPlanEntryResponse entry = Assert.Single(entries);
        Assert.Equal(Date, entry.Date);
        Assert.Equal("Breakfast", entry.MealType);
    }

    [Fact]
    public async Task Create_AllowsDuplicateEntries()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");
        await CreateEntryAsync(client, Date, recipeId, "Breakfast", 1);
        await CreateEntryAsync(client, Date, recipeId, "Breakfast", 1);

        MealPlanEntryResponse[] entries = await ListAsync(client, Date);

        Assert.Equal(2, entries.Length);
        Assert.All(entries, entry =>
        {
            Assert.Equal(recipeId, entry.RecipeId);
            Assert.Equal("Breakfast", entry.MealType);
        });
    }

    [Fact]
    public async Task Create_RejectsMissingRecipe()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/meal-plan",
            new AddMealPlanEntryRequest(Date, Guid.NewGuid(), "Breakfast", 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_RejectsCrossUserRecipe()
    {
        using TestApiFactory factory = new();
        using HttpClient clientA = factory.CreateClientAs(UserA);
        using HttpClient clientB = factory.CreateClientAs(UserB);
        Guid userBRecipeId = await CreateRecipeAsync(clientB, "Private porridge");

        HttpResponseMessage response = await clientA.PostAsJsonAsync(
            "/api/meal-plan",
            new AddMealPlanEntryRequest(Date, userBRecipeId, "Breakfast", 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_RejectsInvalidMealType()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/meal-plan",
            new AddMealPlanEntryRequest(Date, recipeId, "Brunch", 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_RejectsNonPositivePortions()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/meal-plan",
            new AddMealPlanEntryRequest(Date, recipeId, "Breakfast", 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesMealTypeAndPortionsOnly()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");
        Guid entryId = await CreateEntryAsync(client, Date, recipeId, "Breakfast", 1);

        HttpResponseMessage update = await client.PutAsJsonAsync(
            $"/api/meal-plan/{entryId}",
            new UpdateMealPlanEntryRequest("Dinner", 3));
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        MealPlanEntryResponse[] entries = await ListAsync(client, Date);
        MealPlanEntryResponse entry = Assert.Single(entries);
        Assert.Equal(entryId, entry.Id);
        Assert.Equal("Dinner", entry.MealType);
        Assert.Equal(3, entry.Portions);
        // Date and recipe are immutable through update.
        Assert.Equal(Date, entry.Date);
        Assert.Equal(recipeId, entry.RecipeId);
    }

    [Fact]
    public async Task Delete_RemovesEntry()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");
        Guid keepId = await CreateEntryAsync(client, Date, recipeId, "Breakfast", 1);
        Guid deleteId = await CreateEntryAsync(client, Date, recipeId, "Lunch", 1);

        HttpResponseMessage delete = await client.DeleteAsync($"/api/meal-plan/{deleteId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        MealPlanEntryResponse[] entries = await ListAsync(client, Date);
        MealPlanEntryResponse remaining = Assert.Single(entries);
        Assert.Equal(keepId, remaining.Id);
    }

    [Fact]
    public async Task UserB_CannotAccessUserAEntry()
    {
        using TestApiFactory factory = new();
        using HttpClient clientA = factory.CreateClientAs(UserA);
        using HttpClient clientB = factory.CreateClientAs(UserB);
        Guid recipeId = await CreateRecipeAsync(clientA, "Porridge");
        Guid entryId = await CreateEntryAsync(clientA, Date, recipeId, "Breakfast", 1);

        // User B sees nothing for the date and cannot update User A's entry.
        MealPlanEntryResponse[] userBEntries = await ListAsync(clientB, Date);
        Assert.Empty(userBEntries);

        HttpResponseMessage update = await clientB.PutAsJsonAsync(
            $"/api/meal-plan/{entryId}",
            new UpdateMealPlanEntryRequest("Dinner", 5));
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);

        // A cross-user delete is an owner-scoped no-op: User A's entry survives.
        await clientB.DeleteAsync($"/api/meal-plan/{entryId}");
        MealPlanEntryResponse[] userAEntries = await ListAsync(clientA, Date);
        MealPlanEntryResponse entry = Assert.Single(userAEntries);
        Assert.Equal(entryId, entry.Id);
        Assert.Equal("Breakfast", entry.MealType);
        Assert.Equal(1, entry.Portions);
    }

    [Fact]
    public async Task Summary_ReturnsPerEntryMacrosTotalGoalAndSignedRemaining()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid porridgeId = await CreateRecipeAsync(client, "Porridge", 100m, 10m, 5m, 20m);
        Guid soupId = await CreateRecipeAsync(client, "Lentil soup", 300m, 18m, 4m, 40m);
        Guid breakfastId = await CreateEntryAsync(client, Date, porridgeId, "Breakfast", 2);
        Guid lunchId = await CreateEntryAsync(client, Date, soupId, "Lunch", 1);
        await UpsertGoalAsync(client, calories: 450m, protein: 40m, fat: 10m, carbohydrates: 100m);

        DailyMacroSummaryResponse summary = await SummaryAsync(client, Date);

        Assert.Equal(Date, summary.Date);
        Assert.Equal(2, summary.Entries.Count);
        AssertMacros(summary.Total, calories: 500m, protein: 38m, fat: 14m, carbohydrates: 80m);
        Assert.NotNull(summary.Goal);
        AssertMacros(summary.Goal!, calories: 450m, protein: 40m, fat: 10m, carbohydrates: 100m);
        Assert.NotNull(summary.Remaining);
        AssertMacros(summary.Remaining!, calories: -50m, protein: 2m, fat: -4m, carbohydrates: 20m);

        MealEntryMacroResponse breakfast = Assert.Single(summary.Entries, entry => entry.EntryId == breakfastId);
        AssertMacros(breakfast.Macros, calories: 200m, protein: 20m, fat: 10m, carbohydrates: 40m);
        MealEntryMacroResponse lunch = Assert.Single(summary.Entries, entry => entry.EntryId == lunchId);
        AssertMacros(lunch.Macros, calories: 300m, protein: 18m, fat: 4m, carbohydrates: 40m);
    }

    [Fact]
    public async Task Summary_ReturnsNullGoalAndRemaining_WhenNoGoalConfigured()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge", 100m, 10m, 5m, 20m);
        await CreateEntryAsync(client, Date, recipeId, "Breakfast", 1);

        DailyMacroSummaryResponse summary = await SummaryAsync(client, Date);

        AssertMacros(summary.Total, calories: 100m, protein: 10m, fat: 5m, carbohydrates: 20m);
        Assert.Null(summary.Goal);
        Assert.Null(summary.Remaining);
    }

    [Fact]
    public async Task Summary_ReturnsZeroTotalAndEmptyEntries_ForEmptyDay()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        await UpsertGoalAsync(client, calories: 2000m, protein: 150m, fat: 70m, carbohydrates: 200m);

        DailyMacroSummaryResponse summary = await SummaryAsync(client, Date);

        Assert.Empty(summary.Entries);
        AssertMacros(summary.Total, calories: 0m, protein: 0m, fat: 0m, carbohydrates: 0m);
        Assert.NotNull(summary.Goal);
        Assert.NotNull(summary.Remaining);
        AssertMacros(summary.Remaining!, calories: 2000m, protein: 150m, fat: 70m, carbohydrates: 200m);
    }

    [Fact]
    public async Task Summary_IsScopedToCurrentUser()
    {
        using TestApiFactory factory = new();
        using HttpClient clientA = factory.CreateClientAs(UserA);
        using HttpClient clientB = factory.CreateClientAs(UserB);
        Guid userARecipeId = await CreateRecipeAsync(clientA, "User A recipe", 100m, 10m, 5m, 20m);
        Guid userBRecipeId = await CreateRecipeAsync(clientB, "User B recipe", 300m, 30m, 15m, 60m);
        Guid userAEntryId = await CreateEntryAsync(clientA, Date, userARecipeId, "Breakfast", 1);
        await CreateEntryAsync(clientB, Date, userBRecipeId, "Dinner", 1);

        DailyMacroSummaryResponse summary = await SummaryAsync(clientA, Date);

        MealEntryMacroResponse entry = Assert.Single(summary.Entries);
        Assert.Equal(userAEntryId, entry.EntryId);
        AssertMacros(summary.Total, calories: 100m, protein: 10m, fat: 5m, carbohydrates: 20m);
    }

    private static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd");

    private static async Task<MealPlanEntryResponse[]> ListAsync(HttpClient client, DateOnly date)
    {
        MealPlanEntryResponse[]? entries =
            await client.GetFromJsonAsync<MealPlanEntryResponse[]>($"/api/meal-plan?date={Iso(date)}");
        return entries ?? [];
    }

    private static async Task<Guid> CreateEntryAsync(
        HttpClient client,
        DateOnly date,
        Guid recipeId,
        string mealType,
        int portions)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/meal-plan",
            new AddMealPlanEntryRequest(date, recipeId, mealType, portions));
        response.EnsureSuccessStatusCode();
        CreatedMealPlanEntryResponse body =
            (await response.Content.ReadFromJsonAsync<CreatedMealPlanEntryResponse>())!;
        return body.Id;
    }

    private static async Task<Guid> CreateRecipeAsync(HttpClient client, string name)
    {
        return await CreateRecipeAsync(client, name, calories: 100m, protein: 10m, fat: 5m, carbohydrates: 20m);
    }

    private static async Task<Guid> CreateRecipeAsync(
        HttpClient client,
        string name,
        decimal calories,
        decimal protein,
        decimal fat,
        decimal carbohydrates)
    {
        Guid productId = await CreateProductAsync(client, $"{name} product", calories, protein, fat, carbohydrates);
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/recipes",
            new CreateRecipeRequest(name, 1, [new RecipeIngredientRequest(productId, 100m)]));
        response.EnsureSuccessStatusCode();
        CreatedRecipeResponse body = (await response.Content.ReadFromJsonAsync<CreatedRecipeResponse>())!;
        return body.Id;
    }

    private static async Task UpsertGoalAsync(
        HttpClient client,
        decimal calories,
        decimal protein,
        decimal fat,
        decimal carbohydrates)
    {
        HttpResponseMessage response = await client.PutAsJsonAsync(
            "/api/daily-goal",
            new UpsertDailyGoalRequest(calories, protein, fat, carbohydrates));
        response.EnsureSuccessStatusCode();
    }

    private static async Task<DailyMacroSummaryResponse> SummaryAsync(HttpClient client, DateOnly date)
    {
        DailyMacroSummaryResponse? summary =
            await client.GetFromJsonAsync<DailyMacroSummaryResponse>($"/api/meal-plan/summary?date={Iso(date)}");
        return summary!;
    }

    private static void AssertMacros(
        MacroSummaryResponse actual,
        decimal calories,
        decimal protein,
        decimal fat,
        decimal carbohydrates)
    {
        Assert.Equal(calories, actual.Calories);
        Assert.Equal(protein, actual.Protein);
        Assert.Equal(fat, actual.Fat);
        Assert.Equal(carbohydrates, actual.Carbohydrates);
    }

    private static async Task<Guid> CreateProductAsync(
        HttpClient client,
        string name,
        decimal calories,
        decimal protein,
        decimal fat,
        decimal carbohydrates)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/products",
            new CreateProductRequest(name, null, calories, protein, fat, carbohydrates));
        response.EnsureSuccessStatusCode();
        ProductResponse body = (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
        return body.Id;
    }
}
