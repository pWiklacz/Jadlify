using System.Net;
using System.Net.Http.Json;
using Jadlify.API.Planning;
using Jadlify.API.Products;
using Jadlify.API.Recipes;
using Jadlify.API.Shopping;
using Jadlify.API.Tests.Common;
using Jadlify.Domain.Planning;
using Jadlify.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Jadlify.API.Tests.Shopping;

public class ShoppingListEndpointsTests
{
    private const string UserA = "user-a";
    private const string UserB = "user-b";
    private static readonly DateOnly Date = new(2026, 6, 1);

    [Fact]
    public async Task ShoppingListRequiresAuthentication()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        HttpResponseMessage response = await client.GetAsync($"/api/shopping-list?date={Iso(Date)}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShoppingList_ReturnsEmptyDay()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);

        ShoppingListResponse list = await GetShoppingListAsync(client, Date);

        Assert.Equal(Date, list.Date);
        Assert.Empty(list.Items);
        Assert.Empty(list.Warnings);
    }

    [Fact]
    public async Task ShoppingList_AggregatesDuplicateProductsAndScalesByRecipePortions()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid oatsId = await CreateProductAsync(client, "Oats", 370m, 13m, 7m, 60m);
        Guid milkId = await CreateProductAsync(client, "Milk", 64m, 3m, 4m, 5m);
        Guid honeyId = await CreateProductAsync(client, "Honey", 304m, 0m, 0m, 82m);
        Guid porridgeId = await CreateRecipeAsync(
            client,
            "Porridge",
            4,
            [
                new RecipeIngredientRequest(oatsId, 400m),
                new RecipeIngredientRequest(milkId, 800m),
            ]);
        Guid snackId = await CreateRecipeAsync(
            client,
            "Snack",
            2,
            [
                new RecipeIngredientRequest(oatsId, 100m),
                new RecipeIngredientRequest(honeyId, 30m),
            ]);
        await CreateEntryAsync(client, Date, porridgeId, "Breakfast", 2);
        await CreateEntryAsync(client, Date, snackId, "Snack", 1);

        ShoppingListResponse list = await GetShoppingListAsync(client, Date);

        Assert.Empty(list.Warnings);
        Assert.Equal(
            ["Honey", "Milk", "Oats"],
            list.Items.Select(item => item.ProductName).ToArray());
        AssertItem(list, honeyId, "Honey", 15m);
        AssertItem(list, milkId, "Milk", 400m);
        AssertItem(list, oatsId, "Oats", 250m);
    }

    [Fact]
    public async Task ShoppingList_IsScopedToCurrentUser()
    {
        using TestApiFactory factory = new();
        using HttpClient clientA = factory.CreateClientAs(UserA);
        using HttpClient clientB = factory.CreateClientAs(UserB);
        Guid oatsId = await CreateProductAsync(clientA, "Oats", 370m, 13m, 7m, 60m);
        Guid riceId = await CreateProductAsync(clientB, "Rice", 120m, 2m, 1m, 25m);
        Guid userARecipeId = await CreateRecipeAsync(
            clientA,
            "Private oats",
            1,
            [new RecipeIngredientRequest(oatsId, 100m)]);
        Guid userBRecipeId = await CreateRecipeAsync(
            clientB,
            "Private rice",
            1,
            [new RecipeIngredientRequest(riceId, 300m)]);
        await CreateEntryAsync(clientA, Date, userARecipeId, "Breakfast", 1);
        await CreateEntryAsync(clientB, Date, userBRecipeId, "Dinner", 1);

        ShoppingListResponse list = await GetShoppingListAsync(clientA, Date);

        ShoppingListItemResponse item = Assert.Single(list.Items);
        Assert.Equal(oatsId, item.ProductId);
        Assert.Equal("Oats", item.ProductName);
        Assert.Equal(100m, item.Grams);
    }

    [Fact]
    public async Task ShoppingList_ReturnsWarning_WhenMealPlanRecipeIsNotOwnerVisible()
    {
        using TestApiFactory factory = new();
        using HttpClient clientA = factory.CreateClientAs(UserA);
        using HttpClient clientB = factory.CreateClientAs(UserB);
        Guid userBProductId = await CreateProductAsync(clientB, "Private rice", 120m, 2m, 1m, 25m);
        Guid userBRecipeId = await CreateRecipeAsync(
            clientB,
            "Private rice bowl",
            1,
            [new RecipeIngredientRequest(userBProductId, 300m)]);
        Guid entryId = await SeedMealPlanEntryAsync(factory, UserA, userBRecipeId);

        ShoppingListResponse list = await GetShoppingListAsync(clientA, Date);

        Assert.Empty(list.Items);
        ShoppingListWarningResponse warning = Assert.Single(list.Warnings);
        Assert.Equal(entryId, warning.EntryId);
        Assert.Equal(userBRecipeId, warning.RecipeId);
        Assert.Contains("Recipe was not found", warning.Message, StringComparison.Ordinal);
    }

    private static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd");

    private static async Task<ShoppingListResponse> GetShoppingListAsync(HttpClient client, DateOnly date)
    {
        ShoppingListResponse? list =
            await client.GetFromJsonAsync<ShoppingListResponse>($"/api/shopping-list?date={Iso(date)}");
        return list!;
    }

    private static void AssertItem(
        ShoppingListResponse list,
        Guid productId,
        string productName,
        decimal grams)
    {
        ShoppingListItemResponse item = Assert.Single(list.Items, candidate => candidate.ProductId == productId);
        Assert.Equal(productName, item.ProductName);
        Assert.Equal(grams, item.Grams);
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

    private static async Task<Guid> SeedMealPlanEntryAsync(
        TestApiFactory factory,
        string userId,
        Guid recipeId)
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        JadlifyDbContext context = scope.ServiceProvider.GetRequiredService<JadlifyDbContext>();
        var entry = MealPlanEntry.ForRecipe(Guid.NewGuid(), Date, recipeId, MealType.Breakfast, 1);
        context.MealPlanEntries.Add(entry);
        context.Entry(entry).Property("UserId").CurrentValue = userId;
        await context.SaveChangesAsync();
        return entry.Id;
    }

    private static async Task<Guid> CreateRecipeAsync(
        HttpClient client,
        string name,
        int portions,
        IReadOnlyList<RecipeIngredientRequest> ingredients)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/recipes",
            new CreateRecipeRequest(name, portions, ingredients));
        response.EnsureSuccessStatusCode();
        CreatedRecipeResponse body = (await response.Content.ReadFromJsonAsync<CreatedRecipeResponse>())!;
        return body.Id;
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
