using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jadlify.API.Products;
using Jadlify.API.Tests.Common;
using Jadlify.Application.Products;

namespace Jadlify.API.Tests.Products;

public class ProductEndpointsTests
{
    private const string UserA = "user-a";
    private const string UserB = "user-b";

    [Fact]
    public async Task Create_ReturnsCreated_WithLocationAndBody()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);

        CreateProductRequest request = new("Oat flakes", "5901234123457", 370m, 13m, 7m, 60m);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        ProductResponse? body = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
        Assert.Equal("Oat flakes", body.Name);
        Assert.Equal("5901234123457", body.Barcode);
        Assert.Equal(370m, body.Calories);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal($"/api/products/{body.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Get_ReturnsProduct_ForOwner()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid id = await CreateProductAsync(client, new CreateProductRequest("Milk", null, 64m, 3.3m, 3.6m, 4.8m));

        ProductResponse? body = await client.GetFromJsonAsync<ProductResponse>($"/api/products/{id}");

        Assert.NotNull(body);
        Assert.Equal(id, body!.Id);
        Assert.Equal("Milk", body.Name);
        Assert.Null(body.Barcode);
        Assert.Equal(3.3m, body.Protein);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenProductMissing()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);

        HttpResponseMessage response = await client.GetAsync($"/api/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsOnlyOwnProducts()
    {
        using TestApiFactory factory = new();
        using HttpClient clientA = factory.CreateClientAs(UserA);
        using HttpClient clientB = factory.CreateClientAs(UserB);

        await CreateProductAsync(clientA, new CreateProductRequest("A1", null, 1m, 1m, 1m, 1m));
        await CreateProductAsync(clientA, new CreateProductRequest("A2", null, 2m, 2m, 2m, 2m));
        await CreateProductAsync(clientB, new CreateProductRequest("B1", null, 3m, 3m, 3m, 3m));

        ProductResponse[]? list = await clientA.GetFromJsonAsync<ProductResponse[]>("/api/products");

        Assert.NotNull(list);
        Assert.Equal(2, list!.Length);
        Assert.All(list, product => Assert.StartsWith("A", product.Name));
    }

    [Fact]
    public async Task List_AcceptsSearchSkipAndTake()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);

        await CreateProductAsync(client, new CreateProductRequest("Almond milk", "11111111", 40m, 1m, 2.5m, 3m));
        await CreateProductAsync(client, new CreateProductRequest("Cow milk", "22222222", 64m, 3.3m, 3.6m, 4.8m));
        await CreateProductAsync(client, new CreateProductRequest("Oat flakes", "33333333", 370m, 13m, 7m, 60m));

        ProductResponse[]? firstMilkPage =
            await client.GetFromJsonAsync<ProductResponse[]>("/api/products?search=milk&skip=1&take=1");

        Assert.NotNull(firstMilkPage);
        ProductResponse product = Assert.Single(firstMilkPage!);
        Assert.Equal("Cow milk", product.Name);
    }

    [Fact]
    public async Task UserB_CannotAccessUserAProduct_Returns404()
    {
        using TestApiFactory factory = new();
        using HttpClient clientA = factory.CreateClientAs(UserA);
        using HttpClient clientB = factory.CreateClientAs(UserB);

        Guid id = await CreateProductAsync(clientA, new CreateProductRequest("Private", null, 10m, 1m, 1m, 1m));

        HttpResponseMessage get = await clientB.GetAsync($"/api/products/{id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);

        HttpResponseMessage put = await clientB.PutAsJsonAsync(
            $"/api/products/{id}",
            new UpdateProductRequest("Hijacked", null, 5m, 1m, 1m, 1m));
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);

        HttpResponseMessage delete = await clientB.DeleteAsync($"/api/products/{id}");
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);

        // User A's product is untouched by user B's attempts.
        HttpResponseMessage ownerGet = await clientA.GetAsync($"/api/products/{id}");
        Assert.Equal(HttpStatusCode.OK, ownerGet.StatusCode);
    }

    [Fact]
    public async Task Update_ReturnsNoContent_AndReflectsChange()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid id = await CreateProductAsync(client, new CreateProductRequest("Old", "12345678", 100m, 1m, 1m, 1m));

        HttpResponseMessage put = await client.PutAsJsonAsync(
            $"/api/products/{id}",
            new UpdateProductRequest("New", "12345678", 200m, 2m, 2m, 2m));

        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        ProductResponse? body = await client.GetFromJsonAsync<ProductResponse>($"/api/products/{id}");
        Assert.Equal("New", body!.Name);
        Assert.Equal(200m, body.Calories);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenProductMissing()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);

        HttpResponseMessage put = await client.PutAsJsonAsync(
            $"/api/products/{Guid.NewGuid()}",
            new UpdateProductRequest("Ghost", null, 1m, 1m, 1m, 1m));

        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WithFieldErrors_WhenValidationFails()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);

        // Empty name and an impossible calorie density both violate the validator.
        CreateProductRequest request = new("", null, 9000m, 1m, 1m, 1m);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(problem.RootElement.TryGetProperty("errors", out JsonElement errors));
        Assert.True(errors.EnumerateObject().Any());
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_ThenGetReturns404()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);
        Guid id = await CreateProductAsync(client, new CreateProductRequest("Temp", null, 1m, 1m, 1m, 1m));

        HttpResponseMessage delete = await client.DeleteAsync($"/api/products/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        HttpResponseMessage get = await client.GetAsync($"/api/products/{id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenProductMissing()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);

        HttpResponseMessage delete = await client.DeleteAsync($"/api/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task BarcodeLookup_ReturnsFound_WithHttp200_WhenStubHasData()
    {
        using TestApiFactory factory = new();
        factory.BarcodeLookup.OnLookup = _ => new BarcodeProductData(
            Name: "Nutella",
            Brand: "Ferrero",
            Calories: 539m,
            Protein: 6.3m,
            Fat: 30.9m,
            Carbohydrates: 57.5m);
        using HttpClient client = factory.CreateClientAs(UserA);

        HttpResponseMessage response = await client.GetAsync("/api/products/barcode/3017624010701");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        BarcodeLookupResponse? body = await response.Content.ReadFromJsonAsync<BarcodeLookupResponse>();
        Assert.NotNull(body);
        Assert.Equal("Found", body!.Outcome);
        Assert.Equal("3017624010701", body.Barcode);
        Assert.Equal("Nutella", body.Name);
        Assert.Equal(539m, body.Calories);
        Assert.Null(body.ExistingProductId);
    }

    [Fact]
    public async Task BarcodeLookup_ReturnsNotFound_WithHttp200_WhenStubReturnsNull()
    {
        using TestApiFactory factory = new();
        // Default stub returns null (no data).
        using HttpClient client = factory.CreateClientAs(UserA);

        HttpResponseMessage response = await client.GetAsync("/api/products/barcode/00000000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        BarcodeLookupResponse? body = await response.Content.ReadFromJsonAsync<BarcodeLookupResponse>();
        Assert.NotNull(body);
        Assert.Equal("NotFound", body!.Outcome);
        Assert.Equal("00000000", body.Barcode);
        Assert.Null(body.Name);
        Assert.Null(body.ExistingProductId);
    }

    [Fact]
    public async Task Create_RoundTripsPackageSizeAndExtendedFields()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClientAs(UserA);

        CreateProductRequest request = new("Nutella", "3017624010701", 539m, 6.3m, 30.9m, 57.5m)
        {
            PackageSizeGrams = 400m,
            SaturatedFat = 10.6m,
            Sugars = 56.3m,
            Salt = 0.107m,
            VitaminD = 0.000005m,
        };

        HttpResponseMessage create = await client.PostAsJsonAsync("/api/products", request);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        ProductResponse created = (await create.Content.ReadFromJsonAsync<ProductResponse>())!;
        Assert.Equal(400m, created.PackageSizeGrams);
        Assert.Equal(0.000005m, created.VitaminD);

        ProductResponse? fetched = await client.GetFromJsonAsync<ProductResponse>($"/api/products/{created.Id}");

        Assert.NotNull(fetched);
        Assert.Equal(400m, fetched!.PackageSizeGrams);
        Assert.Equal(10.6m, fetched.SaturatedFat);
        Assert.Equal(56.3m, fetched.Sugars);
        Assert.Equal(0.107m, fetched.Salt);
        Assert.Equal(0.000005m, fetched.VitaminD);
        Assert.Null(fetched.Fiber);
    }

    [Fact]
    public async Task BarcodeLookup_ReturnsExtendedFields_WhenStubHasData()
    {
        using TestApiFactory factory = new();
        factory.BarcodeLookup.OnLookup = _ => new BarcodeProductData(
            Name: "Nutella",
            Calories: 539m,
            PackageSizeGrams: 400m,
            SaturatedFat: 10.6m,
            Sugars: 56.3m,
            VitaminD: 0.000005m);
        using HttpClient client = factory.CreateClientAs(UserA);

        BarcodeLookupResponse? body =
            await client.GetFromJsonAsync<BarcodeLookupResponse>("/api/products/barcode/3017624010701");

        Assert.NotNull(body);
        Assert.Equal("Found", body!.Outcome);
        Assert.Equal(400m, body.PackageSizeGrams);
        Assert.Equal(10.6m, body.SaturatedFat);
        Assert.Equal(56.3m, body.Sugars);
        Assert.Equal(0.000005m, body.VitaminD);
        Assert.Null(body.Fiber);
    }

    [Fact]
    public async Task BarcodeLookup_ReturnsAlreadyInCatalog_WhenOwnerHasBarcode()
    {
        using TestApiFactory factory = new();
        // Even though the external source has data, the own-catalog hit must short-circuit.
        factory.BarcodeLookup.OnLookup = _ => new BarcodeProductData(Name: "From external source");
        using HttpClient client = factory.CreateClientAs(UserA);

        Guid id = await CreateProductAsync(
            client,
            new CreateProductRequest("My Yogurt", "1234567890123", 60m, 4m, 3m, 5m));

        BarcodeLookupResponse? body =
            await client.GetFromJsonAsync<BarcodeLookupResponse>("/api/products/barcode/1234567890123");

        Assert.NotNull(body);
        Assert.Equal("AlreadyInCatalog", body!.Outcome);
        Assert.Equal(id, body.ExistingProductId);
        Assert.Equal("My Yogurt", body.Name);
        Assert.Equal(60m, body.Calories);
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client, CreateProductRequest request)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/products", request);
        response.EnsureSuccessStatusCode();
        ProductResponse body = (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
        return body.Id;
    }
}
