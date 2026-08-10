using System.Net;
using System.Net.Http.Json;
using Jadlify.API.Products;
using Jadlify.API.Recipes;
using Jadlify.API.Tests.Common;
using Jadlify.Domain.Planning;
using Jadlify.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Jadlify.API.Tests.Recipes;

public class RecipeEndpointsTests
{
    private const string UserA = "user-a";
    private const string UserB = "user-b";

    [Fact]
    public async Task Create_ReturnsCreated_WithLocation()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid productId = await CreateProductAsync(client, "Oats", 370m, 13m, 7m, 60m);

        CreateRecipeRequest request = new(
            "Porridge",
            2,
            [new RecipeIngredientRequest(productId, 150m)]);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/recipes", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        CreatedRecipeResponse? body = await response.Content.ReadFromJsonAsync<CreatedRecipeResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
        Assert.Equal($"/api/recipes/{body.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Get_ReturnsIngredientSnapshotsAndMacroTotals()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid oatsId = await CreateProductAsync(client, "Oats", 100m, 10m, 5m, 20m);
        Guid milkId = await CreateProductAsync(client, "Milk", 64m, 3m, 4m, 5m);
        Guid recipeId = await CreateRecipeAsync(
            client,
            "Porridge",
            2,
            [
                new RecipeIngredientRequest(oatsId, 150m),
                new RecipeIngredientRequest(milkId, 200m),
            ]);

        RecipeResponse? recipe = await client.GetFromJsonAsync<RecipeResponse>($"/api/recipes/{recipeId}");

        Assert.NotNull(recipe);
        Assert.Equal("Porridge", recipe!.Name);
        Assert.Equal(2, recipe.Portions);
        Assert.Equal(2, recipe.Ingredients.Count);
        Assert.Contains(recipe.Ingredients, ingredient =>
            ingredient.ProductId == oatsId
            && ingredient.ProductName == "Oats"
            && ingredient.WholeRecipeGrams == 150m);
        Assert.Equal(278m, recipe.TotalMacros.Calories);
        Assert.Equal(139m, recipe.PerServingMacros.Calories);
        Assert.Equal(21m, recipe.TotalMacros.Protein);
        Assert.Equal(10.5m, recipe.PerServingMacros.Protein);
    }

    [Fact]
    public async Task List_ReturnsOnlyCurrentUsersRecipes()
    {
        using TestApiFactory factory = new();
        using HttpClient clientA = factory.CreateClientAs(UserA);
        using HttpClient clientB = factory.CreateClientAs(UserB);
        Guid productA = await CreateProductAsync(clientA, "Oats", 100m, 10m, 5m, 20m);
        Guid productB = await CreateProductAsync(clientB, "Rice", 120m, 2m, 1m, 25m);

        await CreateRecipeAsync(clientA, "A recipe", 1, [new RecipeIngredientRequest(productA, 100m)]);
        await CreateRecipeAsync(clientB, "B recipe", 1, [new RecipeIngredientRequest(productB, 100m)]);

        RecipeResponse[]? list = await clientA.GetFromJsonAsync<RecipeResponse[]>("/api/recipes");

        Assert.NotNull(list);
        RecipeResponse recipe = Assert.Single(list!);
        Assert.Equal("A recipe", recipe.Name);
    }

    [Fact]
    public async Task Update_ReplacesRecipeComposition()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid oatsId = await CreateProductAsync(client, "Oats", 100m, 10m, 5m, 20m);
        Guid honeyId = await CreateProductAsync(client, "Honey", 304m, 0m, 0m, 82m);
        Guid recipeId = await CreateRecipeAsync(
            client,
            "Porridge",
            2,
            [new RecipeIngredientRequest(oatsId, 150m)]);

        HttpResponseMessage update = await client.PutAsJsonAsync(
            $"/api/recipes/{recipeId}",
            new UpdateRecipeRequest(
                "Sweet porridge",
                3,
                [
                    new RecipeIngredientRequest(oatsId, 175m),
                    new RecipeIngredientRequest(honeyId, 25m),
                ]));

        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        RecipeResponse? recipe = await client.GetFromJsonAsync<RecipeResponse>($"/api/recipes/{recipeId}");
        Assert.NotNull(recipe);
        Assert.Equal("Sweet porridge", recipe!.Name);
        Assert.Equal(3, recipe.Portions);
        Assert.Equal(2, recipe.Ingredients.Count);
        Assert.Contains(recipe.Ingredients, ingredient =>
            ingredient.ProductId == honeyId && ingredient.ProductName == "Honey");
    }

    [Fact]
    public async Task Delete_ReturnsConflict_WhenRecipeIsUsedByMealPlanEntry()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid productId = await CreateProductAsync(client, "Oats", 100m, 10m, 5m, 20m);
        Guid recipeId = await CreateRecipeAsync(
            client,
            "Porridge",
            1,
            [new RecipeIngredientRequest(productId, 100m)]);

        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope())
        {
            JadlifyDbContext context = scope.ServiceProvider.GetRequiredService<JadlifyDbContext>();
            var entry = MealPlanEntry.ForRecipe(
                Guid.NewGuid(),
                new DateOnly(2026, 6, 1),
                recipeId,
                MealType.Breakfast,
                1);
            context.MealPlanEntries.Add(entry);
            context.Entry(entry).Property("UserId").CurrentValue = UserA;
            await context.SaveChangesAsync();
        }

        HttpResponseMessage response = await client.DeleteAsync($"/api/recipes/{recipeId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UserB_CannotAccessUserARecipe()
    {
        using TestApiFactory factory = new();
        using HttpClient clientA = factory.CreateClientAs(UserA);
        using HttpClient clientB = factory.CreateClientAs(UserB);
        Guid productId = await CreateProductAsync(clientA, "Oats", 100m, 10m, 5m, 20m);
        Guid recipeId = await CreateRecipeAsync(
            clientA,
            "Private recipe",
            1,
            [new RecipeIngredientRequest(productId, 100m)]);

        HttpResponseMessage get = await clientB.GetAsync($"/api/recipes/{recipeId}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);

        HttpResponseMessage update = await clientB.PutAsJsonAsync(
            $"/api/recipes/{recipeId}",
            new UpdateRecipeRequest(
                "Hijacked",
                1,
                [new RecipeIngredientRequest(productId, 100m)]));
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);

        HttpResponseMessage delete = await clientB.DeleteAsync($"/api/recipes/{recipeId}");
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenRecipeBelongsToAnotherUser()
    {
        using TestApiFactory factory = new();
        using HttpClient clientA = factory.CreateClientAs(UserA);
        using HttpClient clientB = factory.CreateClientAs(UserB);
        Guid userAProduct = await CreateProductAsync(clientA, "Oats", 100m, 10m, 5m, 20m);
        Guid userBProduct = await CreateProductAsync(clientB, "Rice", 120m, 2m, 1m, 25m);
        Guid recipeId = await CreateRecipeAsync(
            clientA,
            "Private recipe",
            1,
            [new RecipeIngredientRequest(userAProduct, 100m)]);

        HttpResponseMessage response = await clientB.PutAsJsonAsync(
            $"/api/recipes/{recipeId}",
            new UpdateRecipeRequest(
                "Still private",
                1,
                [new RecipeIngredientRequest(userBProduct, 100m)]));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateRejectsIngredientProductFromAnotherUser()
    {
        using TestApiFactory factory = new();
        using HttpClient clientA = factory.CreateClientAs(UserA);
        using HttpClient clientB = factory.CreateClientAs(UserB);
        Guid userBProduct = await CreateProductAsync(clientB, "Private oats", 100m, 10m, 5m, 20m);

        HttpResponseMessage response = await clientA.PostAsJsonAsync(
            "/api/recipes",
            new CreateRecipeRequest(
                "Cross-user recipe",
                1,
                [new RecipeIngredientRequest(userBProduct, 100m)]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReportsInPlan_ForOwnersPlannedRecipeOnly()
    {
        using TestApiFactory factory = new();
        using HttpClient clientA = factory.CreateClientAs(UserA);
        Guid productId = await CreateProductAsync(clientA, "Oats", 100m, 10m, 5m, 20m);
        Guid plannedId = await CreateRecipeAsync(
            clientA, "Planned", 1, [new RecipeIngredientRequest(productId, 100m)]);
        Guid unplannedId = await CreateRecipeAsync(
            clientA, "Unplanned", 1, [new RecipeIngredientRequest(productId, 100m)]);

        await PlanRecipeAsync(factory, plannedId, UserA);
        // User B planning A's unplanned recipe must not flip A's flag.
        await PlanRecipeAsync(factory, unplannedId, UserB);

        RecipeResponse? planned = await clientA.GetFromJsonAsync<RecipeResponse>($"/api/recipes/{plannedId}");
        RecipeResponse? unplanned = await clientA.GetFromJsonAsync<RecipeResponse>($"/api/recipes/{unplannedId}");

        Assert.True(planned!.IsInPlan);
        Assert.False(unplanned!.IsInPlan);
    }

    [Fact]
    public async Task Catalog_ReturnsSummaryPage_WithTotalAndMacros()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid oatsId = await CreateProductAsync(client, "Oats", 200m, 10m, 5m, 20m);
        // Oracle: 150 g at 200 kcal/100 g = 300 kcal total; 2 portions -> 150 kcal per serving.
        await CreateRecipeAsync(client, "Porridge", 2, [new RecipeIngredientRequest(oatsId, 150m)]);
        await CreateRecipeAsync(client, "Almond bowl", 1, [new RecipeIngredientRequest(oatsId, 100m)]);

        RecipeCatalogResponse? page =
            await client.GetFromJsonAsync<RecipeCatalogResponse>("/api/recipes/catalog?skip=0&take=10");

        Assert.NotNull(page);
        Assert.Equal(2, page!.Total);
        Assert.Equal(0, page.Skip);
        Assert.Equal(10, page.Take);
        // Default sort is alphabetical.
        Assert.Equal(new[] { "Almond bowl", "Porridge" }, page.Items.Select(r => r.Name).ToArray());

        RecipeSummaryResponse porridge = page.Items.Single(r => r.Name == "Porridge");
        Assert.Equal(2, porridge.Portions);
        Assert.Equal(1, porridge.IngredientCount);
        Assert.Equal(300m, porridge.TotalMacros.Calories);
        Assert.Equal(150m, porridge.PerServingMacros.Calories);
        Assert.False(porridge.IsInPlan);
    }

    [Fact]
    public async Task Catalog_SortsByCaloriesPerServing_AcrossTheWholeResultSet()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid productId = await CreateProductAsync(client, "Base", 100m, 10m, 5m, 20m);
        // Per-serving kcal: Alpha 400, Beta 100, Gamma 200 — deliberately the inverse of the
        // alphabetical order, so an in-page sort over a name-ordered window would be caught.
        await CreateRecipeAsync(client, "Alpha", 1, [new RecipeIngredientRequest(productId, 400m)]);
        await CreateRecipeAsync(client, "Beta", 1, [new RecipeIngredientRequest(productId, 100m)]);
        await CreateRecipeAsync(client, "Gamma", 1, [new RecipeIngredientRequest(productId, 200m)]);

        RecipeCatalogResponse? page = await client.GetFromJsonAsync<RecipeCatalogResponse>(
            "/api/recipes/catalog?sort=CaloriesPerServingAsc&take=2");

        Assert.NotNull(page);
        Assert.Equal(3, page!.Total);
        Assert.Equal(new[] { "Beta", "Gamma" }, page.Items.Select(r => r.Name).ToArray());
    }

    [Fact]
    public async Task Catalog_FiltersByNameSearch()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid productId = await CreateProductAsync(client, "Base", 100m, 10m, 5m, 20m);
        await CreateRecipeAsync(client, "Owsianka", 1, [new RecipeIngredientRequest(productId, 100m)]);
        await CreateRecipeAsync(client, "Kurczak", 1, [new RecipeIngredientRequest(productId, 100m)]);

        RecipeCatalogResponse? page =
            await client.GetFromJsonAsync<RecipeCatalogResponse>("/api/recipes/catalog?search=kurcz");

        Assert.NotNull(page);
        Assert.Equal(1, page!.Total);
        Assert.Equal("Kurczak", Assert.Single(page.Items).Name);
    }

    [Fact]
    public async Task Catalog_FlagsRecipesUsedByMealPlanEntries()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid productId = await CreateProductAsync(client, "Oats", 100m, 10m, 5m, 20m);
        Guid plannedId = await CreateRecipeAsync(
            client, "Planned", 1, [new RecipeIngredientRequest(productId, 100m)]);
        await CreateRecipeAsync(client, "Unplanned", 1, [new RecipeIngredientRequest(productId, 100m)]);

        await PlanRecipeAsync(factory, plannedId, UserA);

        RecipeCatalogResponse? page =
            await client.GetFromJsonAsync<RecipeCatalogResponse>("/api/recipes/catalog");

        Assert.NotNull(page);
        Assert.True(page!.Items.Single(r => r.Name == "Planned").IsInPlan);
        Assert.False(page.Items.Single(r => r.Name == "Unplanned").IsInPlan);
    }

    [Fact]
    public async Task Catalog_DoesNotFlagInPlan_ForAnotherUsersMealPlanEntry()
    {
        using TestApiFactory factory = new();
        using HttpClient clientA = factory.CreateClientAs(UserA);
        Guid productId = await CreateProductAsync(clientA, "Oats", 100m, 10m, 5m, 20m);
        Guid recipeId = await CreateRecipeAsync(
            clientA, "Porridge", 1, [new RecipeIngredientRequest(productId, 100m)]);

        // User B plans user A's recipe id: the usage read is owner-scoped, so A's badge stays off.
        await PlanRecipeAsync(factory, recipeId, UserB);

        RecipeCatalogResponse? page =
            await clientA.GetFromJsonAsync<RecipeCatalogResponse>("/api/recipes/catalog");

        Assert.NotNull(page);
        Assert.False(Assert.Single(page!.Items).IsInPlan);
    }

    [Fact]
    public async Task Catalog_ReturnsBadRequest_ForUnknownSort()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);

        HttpResponseMessage response = await client.GetAsync("/api/recipes/catalog?sort=bogus");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Catalog_IsOwnerScoped()
    {
        using TestApiFactory factory = new();
        using HttpClient clientA = factory.CreateClientAs(UserA);
        using HttpClient clientB = factory.CreateClientAs(UserB);
        Guid productId = await CreateProductAsync(clientA, "Oats", 100m, 10m, 5m, 20m);
        await CreateRecipeAsync(clientA, "A-only", 1, [new RecipeIngredientRequest(productId, 100m)]);

        RecipeCatalogResponse? page =
            await clientB.GetFromJsonAsync<RecipeCatalogResponse>("/api/recipes/catalog");

        Assert.NotNull(page);
        Assert.Equal(0, page!.Total);
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task RecipesRequireAuthentication()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        HttpResponseMessage response = await client.GetAsync("/api/recipes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Plants a meal-plan entry for <paramref name="userId"/> straight through the DbContext.</summary>
    private static async Task PlanRecipeAsync(TestApiFactory factory, Guid recipeId, string userId)
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        JadlifyDbContext context = scope.ServiceProvider.GetRequiredService<JadlifyDbContext>();
        var entry = MealPlanEntry.ForRecipe(
            Guid.NewGuid(),
            new DateOnly(2026, 6, 1),
            recipeId,
            MealType.Breakfast,
            1);
        context.MealPlanEntries.Add(entry);
        context.Entry(entry).Property("UserId").CurrentValue = userId;
        await context.SaveChangesAsync();
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
}
