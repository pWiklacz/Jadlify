using System.Net;
using Jadlify.Application.Products;
using Jadlify.Infrastructure.OpenFoodFacts;

namespace Jadlify.Infrastructure.Tests.OpenFoodFacts;

public class OpenFoodFactsBarcodeLookupTests
{
    [Fact]
    public async Task LookupAsync_MapsAllFields_WhenProductFoundWithFullNutriments()
    {
        const string json = """
            {
              "status": 1,
              "product": {
                "product_name": "Nutella",
                "product_name_pl": "Nutella",
                "brands": "Ferrero",
                "quantity": "400 g",
                "nutriments": {
                  "energy-kcal_100g": 539,
                  "proteins_100g": 6.3,
                  "fat_100g": 30.9,
                  "carbohydrates_100g": 57.5
                }
              }
            }
            """;
        OpenFoodFactsBarcodeLookup lookup = CreateLookup(StubHttpMessageHandler.Json(HttpStatusCode.OK, json));

        BarcodeProductData? data = await lookup.LookupAsync("3017624010701");

        Assert.NotNull(data);
        Assert.Equal("Nutella", data.Name);
        Assert.Equal("Ferrero", data.Brand);
        Assert.Equal(539m, data.Calories);
        Assert.Equal(6.3m, data.Protein);
        Assert.Equal(30.9m, data.Fat);
        Assert.Equal(57.5m, data.Carbohydrates);
    }

    [Fact]
    public async Task LookupAsync_PrefersPolishName_WhenPresent()
    {
        const string json = """
            {
              "status": 1,
              "product": {
                "product_name": "Peanut butter",
                "product_name_pl": "Masło orzechowe"
              }
            }
            """;
        OpenFoodFactsBarcodeLookup lookup = CreateLookup(StubHttpMessageHandler.Json(HttpStatusCode.OK, json));

        BarcodeProductData? data = await lookup.LookupAsync("123");

        Assert.NotNull(data);
        Assert.Equal("Masło orzechowe", data.Name);
    }

    [Fact]
    public async Task LookupAsync_FallsBackToDefaultName_WhenPolishNameBlank()
    {
        const string json = """
            {
              "status": 1,
              "product": {
                "product_name": "Peanut butter",
                "product_name_pl": "   "
              }
            }
            """;
        OpenFoodFactsBarcodeLookup lookup = CreateLookup(StubHttpMessageHandler.Json(HttpStatusCode.OK, json));

        BarcodeProductData? data = await lookup.LookupAsync("123");

        Assert.NotNull(data);
        Assert.Equal("Peanut butter", data.Name);
    }

    [Fact]
    public async Task LookupAsync_PreservesBlanks_WhenNutrimentsPartial()
    {
        const string json = """
            {
              "status": 1,
              "product": {
                "product_name": "Mystery food",
                "nutriments": {
                  "proteins_100g": 12
                }
              }
            }
            """;
        OpenFoodFactsBarcodeLookup lookup = CreateLookup(StubHttpMessageHandler.Json(HttpStatusCode.OK, json));

        BarcodeProductData? data = await lookup.LookupAsync("123");

        Assert.NotNull(data);
        Assert.Equal("Mystery food", data.Name);
        Assert.Null(data.Brand);
        Assert.Null(data.Calories);
        Assert.Equal(12m, data.Protein);
        Assert.Null(data.Fat);
        Assert.Null(data.Carbohydrates);
    }

    [Fact]
    public async Task LookupAsync_DerivesCaloriesFromKilojoules_WhenKcalMissing()
    {
        // 1673.6 kJ / 4.184 = 400 kcal exactly.
        const string json = """
            {
              "status": 1,
              "product": {
                "product_name": "Energy by kJ",
                "nutriments": {
                  "energy-kj_100g": 1673.6,
                  "proteins_100g": 5
                }
              }
            }
            """;
        OpenFoodFactsBarcodeLookup lookup = CreateLookup(StubHttpMessageHandler.Json(HttpStatusCode.OK, json));

        BarcodeProductData? data = await lookup.LookupAsync("123");

        Assert.NotNull(data);
        Assert.Equal(400m, data.Calories);
    }

    [Fact]
    public async Task LookupAsync_PrefersKcal_OverKilojoules_WhenBothPresent()
    {
        const string json = """
            {
              "status": 1,
              "product": {
                "product_name": "Both energies",
                "nutriments": {
                  "energy-kcal_100g": 250,
                  "energy-kj_100g": 1673.6
                }
              }
            }
            """;
        OpenFoodFactsBarcodeLookup lookup = CreateLookup(StubHttpMessageHandler.Json(HttpStatusCode.OK, json));

        BarcodeProductData? data = await lookup.LookupAsync("123");

        Assert.NotNull(data);
        Assert.Equal(250m, data.Calories);
    }

    [Fact]
    public async Task LookupAsync_ReturnsNull_WhenBodyStatusIsZero()
    {
        const string json = """
            {
              "code": "0000000000000",
              "status": 0,
              "status_verbose": "product not found"
            }
            """;
        OpenFoodFactsBarcodeLookup lookup = CreateLookup(StubHttpMessageHandler.Json(HttpStatusCode.OK, json));

        BarcodeProductData? data = await lookup.LookupAsync("0000000000000");

        Assert.Null(data);
    }

    [Fact]
    public async Task LookupAsync_ReturnsNull_WhenHttpNotFound()
    {
        OpenFoodFactsBarcodeLookup lookup = CreateLookup(StubHttpMessageHandler.Status(HttpStatusCode.NotFound));

        BarcodeProductData? data = await lookup.LookupAsync("123");

        Assert.Null(data);
    }

    [Fact]
    public async Task LookupAsync_ReturnsNull_WhenHttpRedirect()
    {
        // A 302 means the barcode belongs to a sibling OFF project (e.g. Open Beauty Facts).
        OpenFoodFactsBarcodeLookup lookup = CreateLookup(StubHttpMessageHandler.Status(HttpStatusCode.Redirect));

        BarcodeProductData? data = await lookup.LookupAsync("123");

        Assert.Null(data);
    }

    [Fact]
    public async Task LookupAsync_ReturnsNull_OnTimeout()
    {
        OpenFoodFactsBarcodeLookup lookup =
            CreateLookup(StubHttpMessageHandler.Throws(new TaskCanceledException("timed out")));

        BarcodeProductData? data = await lookup.LookupAsync("123");

        Assert.Null(data);
    }

    [Fact]
    public async Task LookupAsync_ReturnsNull_OnConnectionFailure()
    {
        OpenFoodFactsBarcodeLookup lookup =
            CreateLookup(StubHttpMessageHandler.Throws(new HttpRequestException("connection refused")));

        BarcodeProductData? data = await lookup.LookupAsync("123");

        Assert.Null(data);
    }

    [Fact]
    public async Task LookupAsync_ReturnsNull_OnMalformedJson()
    {
        OpenFoodFactsBarcodeLookup lookup =
            CreateLookup(StubHttpMessageHandler.Json(HttpStatusCode.OK, "{ this is not valid json"));

        BarcodeProductData? data = await lookup.LookupAsync("123");

        Assert.Null(data);
    }

    [Fact]
    public async Task LookupAsync_RequestsTheV2ProductEndpoint_WithFieldsAndBarcode()
    {
        var handler = StubHttpMessageHandler.Json(HttpStatusCode.OK, """{ "status": 0 }""");
        OpenFoodFactsBarcodeLookup lookup = CreateLookup(handler);

        await lookup.LookupAsync("3017624010701");

        Assert.NotNull(handler.LastRequestUri);
        string requested = handler.LastRequestUri.ToString();
        Assert.Contains("/api/v2/product/3017624010701", requested, StringComparison.Ordinal);
        Assert.Contains("fields=", requested, StringComparison.Ordinal);
        Assert.Contains("nutriments", requested, StringComparison.Ordinal);
    }

    private static OpenFoodFactsBarcodeLookup CreateLookup(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://off.test/") });
}
