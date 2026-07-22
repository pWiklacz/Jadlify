using System.Net;
using System.Net.Http.Json;
using Jadlify.API.Planning;
using Jadlify.API.Products;
using Jadlify.API.Recipes;
using Jadlify.API.Tests.Common;

namespace Jadlify.API.Tests.Planning;

public class Phase6ManualSmokeTests
{
    private const string User = "smoke-test-user";
    private static readonly DateOnly SourceDate = new(2026, 6, 1);
    private static readonly DateOnly TargetDate1 = new(2026, 6, 2);
    private static readonly DateOnly TargetDate2 = new(2026, 6, 3);

    [Fact]
    public async Task Smoke_6_3_Move_ReschedulesEntry_WithPreservedIdentity()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(User);

        Guid recipeId = await CreateRecipeAsync(client, "Oatmeal");
        Guid entryId = await CreateEntryAsync(client, SourceDate, recipeId, "Breakfast", 1);

        HttpResponseMessage moveResponse = await client.PostAsJsonAsync(
            $"/api/meal-plan/{entryId}/move",
            new MoveMealPlanEntryRequest(TargetDate1, "Dinner"));

        Assert.Equal(HttpStatusCode.NoContent, moveResponse.StatusCode);

        MealPlanEntryResponse[] sourceEntries = await ListAsync(client, SourceDate);
        Assert.Empty(sourceEntries);

        MealPlanEntryResponse[] targetEntries = await ListAsync(client, TargetDate1);
        MealPlanEntryResponse movedEntry = Assert.Single(targetEntries);
        Assert.Equal(entryId, movedEntry.Id);
        Assert.Equal(TargetDate1, movedEntry.Date);
        Assert.Equal("Dinner", movedEntry.MealType);
        Assert.Equal(1m, movedEntry.Portions);
    }

    [Fact]
    public async Task Smoke_6_3_Copies_CreatesDuplicatesOnTargetDays()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(User);

        Guid recipeId = await CreateRecipeAsync(client, "Scrambled Eggs");
        Guid entryId = await CreateEntryAsync(client, SourceDate, recipeId, "Breakfast", 2);

        HttpResponseMessage copyResponse = await client.PostAsJsonAsync(
            $"/api/meal-plan/{entryId}/copies",
            new CopyMealPlanEntryRequest([TargetDate1, TargetDate2]));

        Assert.Equal(HttpStatusCode.OK, copyResponse.StatusCode);
        CopiedMealPlanEntriesResponse? body = await copyResponse.Content.ReadFromJsonAsync<CopiedMealPlanEntriesResponse>();
        Assert.NotNull(body);
        Assert.Equal(2, body!.Entries.Count);

        // Original preserved
        MealPlanEntryResponse[] sourceEntries = await ListAsync(client, SourceDate);
        Assert.Single(sourceEntries);

        // Target days have copies
        MealPlanEntryResponse[] target1 = await ListAsync(client, TargetDate1);
        Assert.Single(target1);
        Assert.NotEqual(entryId, target1[0].Id);
        Assert.Equal("Breakfast", target1[0].MealType);

        MealPlanEntryResponse[] target2 = await ListAsync(client, TargetDate2);
        Assert.Single(target2);
        Assert.NotEqual(entryId, target2[0].Id);
    }

    [Fact]
    public async Task Smoke_6_3_CopyDay_Add_PreservesTargetDayEntries()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(User);

        Guid recipeId = await CreateRecipeAsync(client, "Chicken Rice");
        await CreateEntryAsync(client, SourceDate, recipeId, "Breakfast", 1);
        await CreateEntryAsync(client, SourceDate, recipeId, "Lunch", 1);

        Guid existingTargetEntryId = await CreateEntryAsync(client, TargetDate1, recipeId, "Dinner", 1);

        HttpResponseMessage copyDayResponse = await client.PostAsJsonAsync(
            $"/api/meal-plan/days/{Iso(SourceDate)}/copies",
            new CopyMealPlanDayRequest([TargetDate1], "Add"));

        Assert.Equal(HttpStatusCode.OK, copyDayResponse.StatusCode);

        MealPlanEntryResponse[] targetEntries = await ListAsync(client, TargetDate1);
        Assert.Equal(3, targetEntries.Length);
        Assert.Contains(targetEntries, e => e.Id == existingTargetEntryId);
    }

    [Fact]
    public async Task Smoke_6_3_CopyDay_Replace_AtomicallyClearsAndOverwritesTargetDay()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(User);

        Guid recipeId = await CreateRecipeAsync(client, "Salad");
        await CreateEntryAsync(client, SourceDate, recipeId, "Breakfast", 1);
        await CreateEntryAsync(client, SourceDate, recipeId, "Lunch", 1);

        Guid existingTargetEntryId = await CreateEntryAsync(client, TargetDate1, recipeId, "Dinner", 1);

        HttpResponseMessage copyDayResponse = await client.PostAsJsonAsync(
            $"/api/meal-plan/days/{Iso(SourceDate)}/copies",
            new CopyMealPlanDayRequest([TargetDate1], "Replace"));

        Assert.Equal(HttpStatusCode.OK, copyDayResponse.StatusCode);

        MealPlanEntryResponse[] targetEntries = await ListAsync(client, TargetDate1);
        Assert.Equal(2, targetEntries.Length);
        Assert.DoesNotContain(targetEntries, e => e.Id == existingTargetEntryId);

        // Source day untouched
        MealPlanEntryResponse[] sourceEntries = await ListAsync(client, SourceDate);
        Assert.Equal(2, sourceEntries.Length);
    }

    [Fact]
    public async Task Smoke_6_4_Rollback_OnInvalidBatch_LeavesZeroPartialChanges()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(User);

        Guid recipeId = await CreateRecipeAsync(client, "Soup");
        await CreateEntryAsync(client, SourceDate, recipeId, "Lunch", 1);
        Guid existingTargetEntryId = await CreateEntryAsync(client, TargetDate1, recipeId, "Dinner", 1);

        // Invalid mode "Merge" -> 400 Bad Request
        HttpResponseMessage invalidModeResponse = await client.PostAsJsonAsync(
            $"/api/meal-plan/days/{Iso(SourceDate)}/copies",
            new CopyMealPlanDayRequest([TargetDate1], "Merge"));

        Assert.Equal(HttpStatusCode.BadRequest, invalidModeResponse.StatusCode);

        // Target day remains untouched
        MealPlanEntryResponse[] targetEntriesAfterInvalidMode = await ListAsync(client, TargetDate1);
        MealPlanEntryResponse remainingEntry = Assert.Single(targetEntriesAfterInvalidMode);
        Assert.Equal(existingTargetEntryId, remainingEntry.Id);

        // Duplicate target days -> 400 Bad Request
        HttpResponseMessage duplicateTargetResponse = await client.PostAsJsonAsync(
            $"/api/meal-plan/days/{Iso(SourceDate)}/copies",
            new CopyMealPlanDayRequest([TargetDate1, TargetDate1], "Add"));

        Assert.Equal(HttpStatusCode.BadRequest, duplicateTargetResponse.StatusCode);

        // Target day remains untouched
        MealPlanEntryResponse[] targetEntriesAfterDuplicates = await ListAsync(client, TargetDate1);
        Assert.Single(targetEntriesAfterDuplicates);
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
        Guid productId = await CreateProductAsync(client, $"{name} product", 100m, 10m, 5m, 20m);
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/recipes",
            new CreateRecipeRequest(name, 1, [new RecipeIngredientRequest(productId, 100m)]));
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
