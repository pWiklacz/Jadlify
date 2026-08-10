using System.Net;
using Jadlify.Application.Products;
using Jadlify.Domain.Products;
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

    [Fact]
    public async Task LookupAsync_MapsExtendedNutrimentsAndProductQuantity()
    {
        const string json = """
            {
              "status": 1,
              "product": {
                "product_name": "Nutella",
                "quantity": "400 g",
                "product_quantity": 400,
                "nutriments": {
                  "energy-kcal_100g": 539,
                  "proteins_100g": 6.3,
                  "fat_100g": 30.9,
                  "carbohydrates_100g": 57.5,
                  "saturated-fat_100g": 10.6,
                  "monounsaturated-fat_100g": 8.1,
                  "polyunsaturated-fat_100g": 4.2,
                  "trans-fat_100g": 0,
                  "sugars_100g": 56.3,
                  "fiber_100g": 0,
                  "salt_100g": 0.107,
                  "sodium_100g": 0.0428,
                  "potassium_100g": 0.4,
                  "calcium_100g": 0.11,
                  "iron_100g": 0.003,
                  "vitamin-a_100g": 0.00012,
                  "vitamin-c_100g": 0,
                  "vitamin-d_100g": 0.0000057
                }
              }
            }
            """;
        OpenFoodFactsBarcodeLookup lookup = CreateLookup(StubHttpMessageHandler.Json(HttpStatusCode.OK, json));

        BarcodeProductData? data = await lookup.LookupAsync("3017624010701");

        Assert.NotNull(data);
        Assert.Equal(400m, data.PackageSizeGrams);
        Assert.Equal(10.6m, data.SaturatedFat);
        Assert.Equal(8.1m, data.MonounsaturatedFat);
        Assert.Equal(4.2m, data.PolyunsaturatedFat);
        Assert.Equal(0m, data.TransFat);
        Assert.Equal(56.3m, data.Sugars);
        Assert.Equal(0m, data.Fiber);
        Assert.Equal(0.107m, data.Salt);
        Assert.Equal(0.0428m, data.Sodium);
        Assert.Equal(0.4m, data.Potassium);
        Assert.Equal(0.11m, data.Calcium);
        Assert.Equal(0.003m, data.Iron);
        Assert.Equal(0.00012m, data.VitaminA);
        Assert.Equal(0m, data.VitaminC);
        Assert.Equal(0.0000057m, data.VitaminD);
    }

    [Fact]
    public async Task LookupAsync_LeavesExtendedFieldsNull_WhenAbsent()
    {
        const string json = """
            {
              "status": 1,
              "product": {
                "product_name": "Bare bones",
                "nutriments": {
                  "energy-kcal_100g": 100,
                  "proteins_100g": 5
                }
              }
            }
            """;
        OpenFoodFactsBarcodeLookup lookup = CreateLookup(StubHttpMessageHandler.Json(HttpStatusCode.OK, json));

        BarcodeProductData? data = await lookup.LookupAsync("123");

        Assert.NotNull(data);
        Assert.Null(data.PackageSizeGrams);
        Assert.Null(data.SaturatedFat);
        Assert.Null(data.Sugars);
        Assert.Null(data.Salt);
        Assert.Null(data.VitaminD);
    }

    [Theory]
    [InlineData("400 g", 400)]
    [InlineData("250g", 250)]
    [InlineData("1.5 kg", 1500)]
    [InlineData("1,5 kg", 1500)]
    public async Task LookupAsync_ParsesPackageSizeFromQuantityText_WhenMass(string quantity, int expectedGrams)
    {
        string json = $$"""
            {
              "status": 1,
              "product": {
                "product_name": "By text quantity",
                "quantity": "{{quantity}}"
              }
            }
            """;
        OpenFoodFactsBarcodeLookup lookup = CreateLookup(StubHttpMessageHandler.Json(HttpStatusCode.OK, json));

        BarcodeProductData? data = await lookup.LookupAsync("123");

        Assert.NotNull(data);
        Assert.Equal((decimal?)expectedGrams, data.PackageSizeGrams);
    }

    [Theory]
    [InlineData("330 ml")]
    [InlineData("1 l")]
    [InlineData("a handful")]
    [InlineData("")]
    public async Task LookupAsync_LeavesPackageSizeNull_ForVolumeOrUnparseableQuantity(string quantity)
    {
        string json = $$"""
            {
              "status": 1,
              "product": {
                "product_name": "Not a mass",
                "quantity": "{{quantity}}"
              }
            }
            """;
        OpenFoodFactsBarcodeLookup lookup = CreateLookup(StubHttpMessageHandler.Json(HttpStatusCode.OK, json));

        BarcodeProductData? data = await lookup.LookupAsync("123");

        Assert.NotNull(data);
        Assert.Null(data.PackageSizeGrams);
    }

    [Fact]
    public async Task LookupAsync_PrefersNumericProductQuantity_OverQuantityText()
    {
        // product_quantity is authoritative grams; the free text disagrees on purpose.
        const string json = """
            {
              "status": 1,
              "product": {
                "product_name": "Mismatch",
                "quantity": "1 kg",
                "product_quantity": 350
              }
            }
            """;
        OpenFoodFactsBarcodeLookup lookup = CreateLookup(StubHttpMessageHandler.Json(HttpStatusCode.OK, json));

        BarcodeProductData? data = await lookup.LookupAsync("123");

        Assert.NotNull(data);
        Assert.Equal(350m, data.PackageSizeGrams);
    }

    [Fact]
    public async Task LookupAsync_SuggestsProductCategory_ForKnownTag()
    {
        const string json = """
            {
              "status": 1,
              "product": {
                "product_name": "Sok pomarańczowy",
                "categories_tags": ["en:plant-based-foods-and-beverages", "en:beverages", "en:juices"]
              }
            }
            """;
        OpenFoodFactsBarcodeLookup lookup = CreateLookup(StubHttpMessageHandler.Json(HttpStatusCode.OK, json));

        BarcodeProductData? data = await lookup.LookupAsync("123");

        Assert.NotNull(data);
        Assert.Equal(ProductCategory.Beverages, data.Category);
    }

    [Fact]
    public async Task LookupAsync_SuggestsProductCategory_FoodTypeBeatsStorageForm()
    {
        // "Frozen vegetables" carries both tags; food type wins, so it suggests Vegetables.
        const string json = """
            {
              "status": 1,
              "product": {
                "product_name": "Mrożona fasolka",
                "categories_tags": ["en:frozen-foods", "en:frozen-vegetables", "en:vegetables"]
              }
            }
            """;
        OpenFoodFactsBarcodeLookup lookup = CreateLookup(StubHttpMessageHandler.Json(HttpStatusCode.OK, json));

        BarcodeProductData? data = await lookup.LookupAsync("123");

        Assert.NotNull(data);
        Assert.Equal(ProductCategory.Vegetables, data.Category);
    }

    [Fact]
    public async Task LookupAsync_LeavesProductCategoryNull_ForUnrecognizedTags()
    {
        const string json = """
            {
              "status": 1,
              "product": {
                "product_name": "Coś dziwnego",
                "categories_tags": ["en:some-obscure-thing"]
              }
            }
            """;
        OpenFoodFactsBarcodeLookup lookup = CreateLookup(StubHttpMessageHandler.Json(HttpStatusCode.OK, json));

        BarcodeProductData? data = await lookup.LookupAsync("123");

        Assert.NotNull(data);
        Assert.Null(data.Category);
    }

    [Fact]
    public async Task LookupAsync_LeavesProductCategoryNull_WhenTagsAbsent()
    {
        const string json = """
            {
              "status": 1,
              "product": {
                "product_name": "Bez tagów"
              }
            }
            """;
        OpenFoodFactsBarcodeLookup lookup = CreateLookup(StubHttpMessageHandler.Json(HttpStatusCode.OK, json));

        BarcodeProductData? data = await lookup.LookupAsync("123");

        Assert.NotNull(data);
        Assert.Null(data.Category);
    }

    private static OpenFoodFactsBarcodeLookup CreateLookup(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://off.test/") });
}
