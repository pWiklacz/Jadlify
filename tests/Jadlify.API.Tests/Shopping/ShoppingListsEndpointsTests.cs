using System.Net;
using System.Net.Http.Json;
using Jadlify.API.Planning;
using Jadlify.API.Products;
using Jadlify.API.Recipes;
using Jadlify.API.Shopping;
using Jadlify.API.Tests.Common;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Jadlify.API.Tests.Shopping;

public class ShoppingListsEndpointsTests
{
    private const string UserA = "user-a";
    private const string UserB = "user-b";
    private static readonly DateOnly Day = new(2026, 6, 6);

    [Fact]
    public async Task ShoppingListsRequireAuthentication()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        HttpResponseMessage response = await client.GetAsync("/api/shopping-lists");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsCreated_WithItemsProjectedFromThePlan()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        await SeedPlanAsync(client, portions: 1);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/shopping-lists",
            new CreateShoppingListRequest("Weekend", [Day]));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        ShoppingListDetailResponse body = (await response.Content.ReadFromJsonAsync<ShoppingListDetailResponse>())!;
        Assert.Equal($"/api/shopping-lists/{body.Id}", response.Headers.Location!.ToString());
        Assert.Equal("Active", body.Status);
        ShoppingListItemDetailResponse item = Assert.Single(body.Items);
        Assert.Equal(200m, item.Grams); // one portion of a 200 g whole-recipe ingredient
        Assert.False(item.IsBought);
    }

    [Fact]
    public async Task Create_RejectsASecondActiveList()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        await SeedPlanAsync(client, 1);
        await CreateListAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/shopping-lists",
            new CreateShoppingListRequest("Another", [Day]));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_RejectsEmptyDays()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/shopping-lists",
            new CreateShoppingListRequest("Empty", []));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Toggle_MarksAnItemBought()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        await SeedPlanAsync(client, 1);
        ShoppingListDetailResponse list = await CreateListAsync(client);
        ShoppingListItemDetailResponse item = list.Items[0];

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/shopping-lists/{list.Id}/items/{item.Id}",
            new ToggleShoppingListItemRequest(true, list.Version));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ShoppingListDetailResponse body = (await response.Content.ReadFromJsonAsync<ShoppingListDetailResponse>())!;
        Assert.True(body.Items.Single(i => i.Id == item.Id).IsBought);
    }

    [Fact]
    public async Task Toggle_ReturnsConflict_OnStaleVersion()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        await SeedPlanAsync(client, 1);
        ShoppingListDetailResponse list = await CreateListAsync(client);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/shopping-lists/{list.Id}/items/{list.Items[0].Id}",
            new ToggleShoppingListItemRequest(true, list.Version + 1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Diff_ThenRefresh_AppliesThePlanChange()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid entryId = await SeedPlanAsync(client, portions: 1);
        ShoppingListDetailResponse list = await CreateListAsync(client);

        // Double the planned portions, so the list needs twice the grams.
        HttpResponseMessage update = await client.PutAsJsonAsync(
            $"/api/meal-plan/{entryId}",
            new UpdateMealPlanEntryRequest("Breakfast", Portions: 2));
        update.EnsureSuccessStatusCode();

        ShoppingListDiffResponse diff = (await client.GetFromJsonAsync<ShoppingListDiffResponse>(
            $"/api/shopping-lists/{list.Id}/diff"))!;
        Assert.True(diff.HasChanges);
        Assert.Single(diff.Changed);

        HttpResponseMessage refresh = await client.PostAsJsonAsync(
            $"/api/shopping-lists/{list.Id}/refresh",
            new RefreshShoppingListRequest(diff.ListVersion, diff.SourceFingerprint));

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        ShoppingListDetailResponse body = (await refresh.Content.ReadFromJsonAsync<ShoppingListDetailResponse>())!;
        Assert.Equal(400m, body.Items.Single().Grams);
        Assert.Equal(list.Version + 1, body.Version);
    }

    [Fact]
    public async Task Complete_MovesTheListToHistory()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        await SeedPlanAsync(client, 1);
        ShoppingListDetailResponse list = await CreateListAsync(client);

        HttpResponseMessage complete = await client.PostAsJsonAsync(
            $"/api/shopping-lists/{list.Id}/complete",
            new CompleteShoppingListRequest(list.Version));
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);

        ShoppingListIndexResponse index = (await client.GetFromJsonAsync<ShoppingListIndexResponse>(
            "/api/shopping-lists"))!;
        Assert.Null(index.Active);
        Assert.Equal(list.Id, Assert.Single(index.History).Id);
    }

    [Fact]
    public async Task GetById_IsOwnerScoped()
    {
        using TestApiFactory factory = new();
        using HttpClient owner = factory.CreateClientAs(UserA);
        await SeedPlanAsync(owner, 1);
        ShoppingListDetailResponse list = await CreateListAsync(owner);

        using HttpClient other = factory.CreateClientAs(UserB);
        HttpResponseMessage response = await other.GetAsync($"/api/shopping-lists/{list.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<ShoppingListDetailResponse> CreateListAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/shopping-lists",
            new CreateShoppingListRequest("List", [Day]));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ShoppingListDetailResponse>())!;
    }

    // Seeds a product, a one-ingredient recipe (200 g whole-recipe), and one meal-plan entry on
    // Day; returns the entry id so a test can change its portions. Returns after the plan exists.
    private static async Task<Guid> SeedPlanAsync(HttpClient client, decimal portions)
    {
        Guid productId = await CreateProductAsync(client, "Oats");
        Guid recipeId = await CreateRecipeAsync(client, "Porridge", productId);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/meal-plan",
            new AddMealPlanEntryRequest(Day, "Breakfast", RecipeId: recipeId, Portions: portions));
        response.EnsureSuccessStatusCode();
        CreatedMealPlanEntryResponse body = (await response.Content.ReadFromJsonAsync<CreatedMealPlanEntryResponse>())!;
        return body.Id;
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client, string name)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/products",
            new CreateProductRequest(name, null, 380m, 13m, 7m, 60m));
        response.EnsureSuccessStatusCode();
        ProductResponse body = (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
        return body.Id;
    }

    private static async Task<Guid> CreateRecipeAsync(HttpClient client, string name, Guid productId)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/recipes",
            new CreateRecipeRequest(name, 1, [new RecipeIngredientRequest(productId, 200m)]));
        response.EnsureSuccessStatusCode();
        CreatedRecipeResponse body = (await response.Content.ReadFromJsonAsync<CreatedRecipeResponse>())!;
        return body.Id;
    }
}
