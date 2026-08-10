using System.Net;
using System.Net.Http.Json;
using Jadlify.API.Planning;
using Jadlify.API.Products;
using Jadlify.API.Recipes;
using Jadlify.API.Tests.Common;
using Jadlify.Application.Planning;
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
            new AddMealPlanEntryRequest(Date, "Breakfast", RecipeId: recipeId, Portions: 1));

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
        Assert.Equal(2m, entry.Portions);
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
            new AddMealPlanEntryRequest(Date, "Breakfast", RecipeId: Guid.NewGuid(), Portions: 1));

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
            new AddMealPlanEntryRequest(Date, "Breakfast", RecipeId: userBRecipeId, Portions: 1));

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
            new AddMealPlanEntryRequest(Date, "Brunch", RecipeId: recipeId, Portions: 1));

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
            new AddMealPlanEntryRequest(Date, "Breakfast", RecipeId: recipeId, Portions: 0));

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
            new UpdateMealPlanEntryRequest("Dinner", Portions: 3));
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        MealPlanEntryResponse[] entries = await ListAsync(client, Date);
        MealPlanEntryResponse entry = Assert.Single(entries);
        Assert.Equal(entryId, entry.Id);
        Assert.Equal("Dinner", entry.MealType);
        Assert.Equal(3m, entry.Portions);
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
            new UpdateMealPlanEntryRequest("Dinner", Portions: 5));
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);

        // A cross-user delete is an owner-scoped no-op: User A's entry survives.
        await clientB.DeleteAsync($"/api/meal-plan/{entryId}");
        MealPlanEntryResponse[] userAEntries = await ListAsync(clientA, Date);
        MealPlanEntryResponse entry = Assert.Single(userAEntries);
        Assert.Equal(entryId, entry.Id);
        Assert.Equal("Breakfast", entry.MealType);
        Assert.Equal(1m, entry.Portions);
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

    [Fact]
    public async Task Create_AcceptsHalfPortionsForARecipe()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/meal-plan",
            new AddMealPlanEntryRequest(Date, "Snack", RecipeId: recipeId, Portions: 0.5m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        MealPlanEntryResponse entry = Assert.Single(await ListAsync(client, Date));
        Assert.Equal("Recipe", entry.Source);
        Assert.Equal(0.5m, entry.Portions);
    }

    [Theory]
    [InlineData(0.25)]
    [InlineData(1.1)]
    public async Task Create_RejectsOffStepPortions(decimal portions)
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/meal-plan",
            new AddMealPlanEntryRequest(Date, "Breakfast", RecipeId: recipeId, Portions: portions));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_AddsProductEntry_CarryingItsOwnSnapshot()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid productId = await CreateProductAsync(client, "Oats", 380m, 13m, 7m, 60m);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/meal-plan",
            new AddMealPlanEntryRequest(Date, "Snack", ProductId: productId, Grams: 45m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        MealPlanEntryResponse entry = Assert.Single(await ListAsync(client, Date));
        Assert.Equal("Product", entry.Source);
        Assert.Equal(productId, entry.ProductId);
        Assert.Equal("Oats", entry.ProductName);
        Assert.Equal(45m, entry.Grams);
        Assert.Null(entry.RecipeId);
        Assert.Null(entry.Portions);
    }

    [Fact]
    public async Task Create_RejectsAmbiguousSource()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");
        Guid productId = await CreateProductAsync(client, "Oats", 380m, 13m, 7m, 60m);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/meal-plan",
            new AddMealPlanEntryRequest(
                Date,
                "Breakfast",
                RecipeId: recipeId,
                Portions: 1m,
                ProductId: productId,
                Grams: 45m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await ListAsync(client, Date));
    }

    [Fact]
    public async Task Create_RejectsEmptySource()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/meal-plan",
            new AddMealPlanEntryRequest(Date, "Breakfast"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_RejectsCrossUserProduct()
    {
        using TestApiFactory factory = new();
        using HttpClient clientA = factory.CreateClientAs(UserA);
        using HttpClient clientB = factory.CreateClientAs(UserB);
        Guid userBProductId = await CreateProductAsync(clientB, "Private oats", 380m, 13m, 7m, 60m);

        HttpResponseMessage response = await clientA.PostAsJsonAsync(
            "/api/meal-plan",
            new AddMealPlanEntryRequest(Date, "Snack", ProductId: userBProductId, Grams: 45m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProductEntry_SurvivesItsCatalogProductBeingDeleted()
    {
        // The snapshot is the whole point: a past day must not change when the catalog does.
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid productId = await CreateProductAsync(client, "Oats", 380m, 13m, 7m, 60m);
        await client.PostAsJsonAsync(
            "/api/meal-plan",
            new AddMealPlanEntryRequest(Date, "Snack", ProductId: productId, Grams: 100m));

        HttpResponseMessage delete = await client.DeleteAsync($"/api/products/{productId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        MealPlanEntryResponse entry = Assert.Single(await ListAsync(client, Date));
        Assert.Equal("Oats", entry.ProductName);
        DailyMacroSummaryResponse summary = await SummaryAsync(client, Date);
        AssertMacros(summary.Total, calories: 380m, protein: 13m, fat: 7m, carbohydrates: 60m);
    }

    [Fact]
    public async Task Update_RejectsGramsForARecipeEntry()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");
        Guid entryId = await CreateEntryAsync(client, Date, recipeId, "Breakfast", 1);

        HttpResponseMessage update = await client.PutAsJsonAsync(
            $"/api/meal-plan/{entryId}",
            new UpdateMealPlanEntryRequest("Dinner", Grams: 100m));

        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        Assert.Equal(1m, Assert.Single(await ListAsync(client, Date)).Portions);
    }

    [Fact]
    public async Task Summary_TotalsMixedRecipeAndProductEntries()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge", 100m, 10m, 5m, 20m);
        Guid productId = await CreateProductAsync(client, "Oats", 380m, 13m, 7m, 60m);
        await CreateEntryAsync(client, Date, recipeId, "Breakfast", 1);
        await client.PostAsJsonAsync(
            "/api/meal-plan",
            new AddMealPlanEntryRequest(Date, "Snack", ProductId: productId, Grams: 50m));

        DailyMacroSummaryResponse summary = await SummaryAsync(client, Date);

        // Oracle: recipe 100 kcal + 380 kcal/100 g * 50 g (190 kcal) = 290 kcal.
        Assert.Equal(2, summary.Entries.Count);
        AssertMacros(summary.Total, calories: 290m, protein: 16.5m, fat: 8.5m, carbohydrates: 50m);
    }

    [Fact]
    public async Task Range_ReturnsEveryDayInclusiveOfBothBounds()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge", 100m, 10m, 5m, 20m);
        await CreateEntryAsync(client, Date, recipeId, "Breakfast", 1);
        await CreateEntryAsync(client, Date.AddDays(2), recipeId, "Dinner", 2);

        MealPlanRangeResponse range = await RangeAsync(client, Date, Date.AddDays(2));

        Assert.Equal(3, range.Days.Count);
        Assert.Equal(Date, range.Days[0].Date);
        Assert.Equal(Date.AddDays(2), range.Days[^1].Date);
        Assert.Single(range.Days[0].Entries);
        Assert.Empty(range.Days[1].Entries);
        AssertMacros(range.Days[0].Total, calories: 100m, protein: 10m, fat: 5m, carbohydrates: 20m);
        AssertMacros(range.Days[1].Total, calories: 0m, protein: 0m, fat: 0m, carbohydrates: 0m);
        AssertMacros(range.Days[2].Total, calories: 200m, protein: 20m, fat: 10m, carbohydrates: 40m);
    }

    [Fact]
    public async Task Range_ReturnsFullMonthGridOfDays()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);

        MealPlanRangeResponse range = await RangeAsync(client, Date, Date.AddDays(41));

        Assert.Equal(42, range.Days.Count);
    }

    [Fact]
    public async Task Range_RejectsWindowBeyondMaximum()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);

        HttpResponseMessage response = await client.GetAsync(
            $"/api/meal-plan/range?from={Iso(Date)}&to={Iso(Date.AddDays(42))}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Range_RejectsInvertedWindow()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);

        HttpResponseMessage response = await client.GetAsync(
            $"/api/meal-plan/range?from={Iso(Date)}&to={Iso(Date.AddDays(-1))}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Range_RequiresAuthentication()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        HttpResponseMessage response = await client.GetAsync(
            $"/api/meal-plan/range?from={Iso(Date)}&to={Iso(Date)}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Range_IsScopedToCurrentUser()
    {
        using TestApiFactory factory = new();
        using HttpClient clientA = factory.CreateClientAs(UserA);
        using HttpClient clientB = factory.CreateClientAs(UserB);
        Guid userBRecipeId = await CreateRecipeAsync(clientB, "User B recipe", 300m, 30m, 15m, 60m);
        await CreateEntryAsync(clientB, Date, userBRecipeId, "Dinner", 1);

        MealPlanRangeResponse range = await RangeAsync(clientA, Date, Date.AddDays(6));

        Assert.All(range.Days, day => Assert.Empty(day.Entries));
    }

    [Fact]
    public async Task Range_CarriesGoalAndSignedRemainingOnEveryDay()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge", 100m, 10m, 5m, 20m);
        await CreateEntryAsync(client, Date, recipeId, "Breakfast", 2);
        await UpsertGoalAsync(client, calories: 150m, protein: 40m, fat: 10m, carbohydrates: 100m);

        MealPlanRangeResponse range = await RangeAsync(client, Date, Date.AddDays(1));

        Assert.NotNull(range.Days[0].Goal);
        // Oracle: 200 kcal planned against a 150 kcal goal leaves -50 remaining.
        Assert.Equal(-50m, range.Days[0].Remaining!.Calories);
        // An empty day still carries the goal, with the whole target outstanding.
        Assert.Equal(150m, range.Days[1].Remaining!.Calories);
    }

    [Fact]
    public async Task MoveRequiresAuthentication()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/meal-plan/{Guid.NewGuid()}/move",
            new MoveMealPlanEntryRequest(Date, "Lunch"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Move_ReschedulesEntry_KeepingItsIdAndQuantity()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");
        Guid entryId = await CreateEntryAsync(client, Date, recipeId, "Breakfast", 2);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/meal-plan/{entryId}/move",
            new MoveMealPlanEntryRequest(OtherDate, "Dinner"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await ListAsync(client, Date));
        MealPlanEntryResponse moved = Assert.Single(await ListAsync(client, OtherDate));
        Assert.Equal(entryId, moved.Id);
        Assert.Equal("Dinner", moved.MealType);
        Assert.Equal(2m, moved.Portions);
    }

    [Fact]
    public async Task Move_ReturnsNotFound_ForAnotherUsersEntry()
    {
        using TestApiFactory factory = new();
        using HttpClient clientA = factory.CreateClientAs(UserA);
        using HttpClient clientB = factory.CreateClientAs(UserB);
        Guid recipeId = await CreateRecipeAsync(clientA, "Porridge");
        Guid entryId = await CreateEntryAsync(clientA, Date, recipeId, "Breakfast", 1);

        HttpResponseMessage response = await clientB.PostAsJsonAsync(
            $"/api/meal-plan/{entryId}/move",
            new MoveMealPlanEntryRequest(OtherDate, "Dinner"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(Date, Assert.Single(await ListAsync(clientA, Date)).Date);
    }

    [Fact]
    public async Task Move_RejectsUnknownMealType()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");
        Guid entryId = await CreateEntryAsync(client, Date, recipeId, "Breakfast", 1);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/meal-plan/{entryId}/move",
            new MoveMealPlanEntryRequest(OtherDate, "Brunch"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Single(await ListAsync(client, Date));
    }

    [Fact]
    public async Task Copies_CreateOneEntryPerTargetDay_LeavingTheOriginal()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");
        Guid entryId = await CreateEntryAsync(client, Date, recipeId, "Breakfast", 2);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/meal-plan/{entryId}/copies",
            new CopyMealPlanEntryRequest([Date.AddDays(1), Date.AddDays(2)]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        CopiedMealPlanEntriesResponse body =
            (await response.Content.ReadFromJsonAsync<CopiedMealPlanEntriesResponse>())!;
        Assert.Equal(2, body.Entries.Count);
        Assert.Equal([Date.AddDays(1), Date.AddDays(2)], body.Entries.Select(entry => entry.Date).ToList());
        Assert.DoesNotContain(body.Entries, entry => entry.Id == entryId);

        Assert.Equal(entryId, Assert.Single(await ListAsync(client, Date)).Id);
        MealPlanEntryResponse copy = Assert.Single(await ListAsync(client, Date.AddDays(1)));
        Assert.Equal("Breakfast", copy.MealType);
        Assert.Equal(2m, copy.Portions);
    }

    [Fact]
    public async Task Copies_ReturnNotFound_ForAnotherUsersEntry()
    {
        using TestApiFactory factory = new();
        using HttpClient clientA = factory.CreateClientAs(UserA);
        using HttpClient clientB = factory.CreateClientAs(UserB);
        Guid recipeId = await CreateRecipeAsync(clientA, "Porridge");
        Guid entryId = await CreateEntryAsync(clientA, Date, recipeId, "Breakfast", 1);

        HttpResponseMessage response = await clientB.PostAsJsonAsync(
            $"/api/meal-plan/{entryId}/copies",
            new CopyMealPlanEntryRequest([Date.AddDays(1)]));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await ListAsync(clientB, Date.AddDays(1)));
        Assert.Empty(await ListAsync(clientA, Date.AddDays(1)));
    }

    [Fact]
    public async Task Copies_RejectRepeatedTargetDays()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");
        Guid entryId = await CreateEntryAsync(client, Date, recipeId, "Breakfast", 1);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/meal-plan/{entryId}/copies",
            new CopyMealPlanEntryRequest([Date.AddDays(1), Date.AddDays(1)]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await ListAsync(client, Date.AddDays(1)));
    }

    [Fact]
    public async Task Copies_RejectMoreTargetDaysThanTheLimit()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");
        Guid entryId = await CreateEntryAsync(client, Date, recipeId, "Breakfast", 1);
        DateOnly[] targets =
        [
            .. Enumerable
                .Range(1, PlanningValidationBounds.MaxCopyTargetDays + 1)
                .Select(offset => Date.AddDays(offset))
        ];

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/meal-plan/{entryId}/copies",
            new CopyMealPlanEntryRequest(targets));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await ListAsync(client, Date.AddDays(1)));
    }

    [Fact]
    public async Task CopyDay_Add_KeepsWhatTheTargetDayAlreadyHeld()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");
        await CreateEntryAsync(client, Date, recipeId, "Breakfast", 1);
        await CreateEntryAsync(client, Date, recipeId, "Lunch", 1);
        Guid existingId = await CreateEntryAsync(client, OtherDate, recipeId, "Dinner", 1);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/meal-plan/days/{Iso(Date)}/copies",
            new CopyMealPlanDayRequest([OtherDate], "Add"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        CopiedMealPlanEntriesResponse body =
            (await response.Content.ReadFromJsonAsync<CopiedMealPlanEntriesResponse>())!;
        Assert.Equal(2, body.Entries.Count);

        MealPlanEntryResponse[] target = await ListAsync(client, OtherDate);
        Assert.Equal(3, target.Length);
        Assert.Contains(target, entry => entry.Id == existingId);
    }

    [Fact]
    public async Task CopyDay_Replace_LeavesOnlyTheCopies()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");
        await CreateEntryAsync(client, Date, recipeId, "Breakfast", 1);
        await CreateEntryAsync(client, Date, recipeId, "Lunch", 1);
        Guid replacedId = await CreateEntryAsync(client, OtherDate, recipeId, "Dinner", 1);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/meal-plan/days/{Iso(Date)}/copies",
            new CopyMealPlanDayRequest([OtherDate], "Replace"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        MealPlanEntryResponse[] target = await ListAsync(client, OtherDate);
        Assert.Equal(2, target.Length);
        Assert.DoesNotContain(target, entry => entry.Id == replacedId);
        // The source day is untouched by either mode.
        Assert.Equal(2, (await ListAsync(client, Date)).Length);
    }

    [Fact]
    public async Task CopyDay_Replace_DoesNotClearAnotherUsersDay()
    {
        using TestApiFactory factory = new();
        using HttpClient clientA = factory.CreateClientAs(UserA);
        using HttpClient clientB = factory.CreateClientAs(UserB);
        Guid recipeA = await CreateRecipeAsync(clientA, "Porridge");
        Guid recipeB = await CreateRecipeAsync(clientB, "Salad");
        await CreateEntryAsync(clientA, Date, recipeA, "Breakfast", 1);
        Guid userBEntryId = await CreateEntryAsync(clientB, OtherDate, recipeB, "Dinner", 1);

        HttpResponseMessage response = await clientA.PostAsJsonAsync(
            $"/api/meal-plan/days/{Iso(Date)}/copies",
            new CopyMealPlanDayRequest([OtherDate], "Replace"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(userBEntryId, Assert.Single(await ListAsync(clientB, OtherDate)).Id);
    }

    [Fact]
    public async Task CopyDay_RejectsEmptySourceDay_WithoutClearingTheTarget()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");
        Guid existingId = await CreateEntryAsync(client, OtherDate, recipeId, "Dinner", 1);

        // Replace from an empty day would otherwise be a silent way to wipe the target.
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/meal-plan/days/{Iso(Date)}/copies",
            new CopyMealPlanDayRequest([OtherDate], "Replace"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(existingId, Assert.Single(await ListAsync(client, OtherDate)).Id);
    }

    [Fact]
    public async Task CopyDay_RejectsSourceDayAmongTargets()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");
        await CreateEntryAsync(client, Date, recipeId, "Breakfast", 1);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/meal-plan/days/{Iso(Date)}/copies",
            new CopyMealPlanDayRequest([OtherDate, Date], "Add"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Single(await ListAsync(client, Date));
        Assert.Empty(await ListAsync(client, OtherDate));
    }

    [Fact]
    public async Task CopyDay_RejectsUnknownMode()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid recipeId = await CreateRecipeAsync(client, "Porridge");
        await CreateEntryAsync(client, Date, recipeId, "Breakfast", 1);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/meal-plan/days/{Iso(Date)}/copies",
            new CopyMealPlanDayRequest([OtherDate], "Merge"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await ListAsync(client, OtherDate));
    }

    [Fact]
    public async Task CopyDay_CopiesProductEntriesWithTheirOwnSnapshot()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid productId = await CreateProductAsync(client, "Oats", 380m, 13m, 7m, 60m);
        HttpResponseMessage created = await client.PostAsJsonAsync(
            "/api/meal-plan",
            new AddMealPlanEntryRequest(Date, "Snack", ProductId: productId, Grams: 45m));
        created.EnsureSuccessStatusCode();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/meal-plan/days/{Iso(Date)}/copies",
            new CopyMealPlanDayRequest([OtherDate], "Add"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        MealPlanEntryResponse copy = Assert.Single(await ListAsync(client, OtherDate));
        Assert.Equal("Product", copy.Source);
        Assert.Equal(productId, copy.ProductId);
        Assert.Equal("Oats", copy.ProductName);
        Assert.Equal(45m, copy.Grams);
    }

    private static async Task<MealPlanRangeResponse> RangeAsync(HttpClient client, DateOnly from, DateOnly to)
    {
        MealPlanRangeResponse? range = await client.GetFromJsonAsync<MealPlanRangeResponse>(
            $"/api/meal-plan/range?from={Iso(from)}&to={Iso(to)}");
        return range!;
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
            new AddMealPlanEntryRequest(date, mealType, RecipeId: recipeId, Portions: portions));
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
