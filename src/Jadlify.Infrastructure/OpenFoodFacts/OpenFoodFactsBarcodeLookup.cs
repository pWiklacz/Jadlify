using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Jadlify.Application.Products;
using Jadlify.Domain.Products;

namespace Jadlify.Infrastructure.OpenFoodFacts;

/// <summary>
/// <see cref="IBarcodeProductLookup"/> backed by the Open Food Facts v2 get-by-barcode
/// endpoint. A thin, swappable adapter: it maps the OFF envelope to
/// <see cref="BarcodeProductData"/> and converts <em>every</em> failure mode — HTTP
/// timeout, non-2xx (incl. 404/302), malformed JSON, and body <c>status: 0</c> — into a
/// single <c>null</c> "no data" result, so a barcode lookup can never block product
/// creation (FR-006 / off-api-reference §5, §12).
/// </summary>
internal sealed class OpenFoodFactsBarcodeLookup : IBarcodeProductLookup
{
    // OFF expresses energy in kJ; divide by this to derive kcal when the kcal key is absent.
    private const decimal KilojoulesPerKilocalorie = 4.184m;

    // Only request the fields we snapshot — a full product object is huge (off-api-reference §4).
    private const string Fields =
        "product_name,product_name_pl,brands,quantity,product_quantity,categories_tags,nutriments";

    // Best-effort taxonomy suggestion from OFF category tags (e.g. "en:vegetables"). Tags are
    // lowercase, language-prefixed, and hierarchical; the rules are scanned in a fixed precedence
    // (food type before storage form, so "frozen vegetables" suggests Vegetables). The first
    // keyword hit wins; an unrecognized set maps to null — never an error (FR-006).
    private static readonly (string[] Keywords, ProductCategory Category)[] CategoryRules =
    [
        (["vegetable"], ProductCategory.Vegetables),
        (["fruit", "berries"], ProductCategory.Fruits),
        (["meat", "poultry", "chicken", "beef", "pork", "fish", "seafood", "sausage"], ProductCategory.MeatAndFish),
        (["dairy", "dairies", "milk", "cheese", "yogurt", "yoghurt"], ProductCategory.Dairy),
        (["cereal", "bread", "pasta", "rice", "grain", "bakery"], ProductCategory.GrainsAndBread),
        (["beverage", "drink", "water", "juice", "soda", "tea", "coffee"], ProductCategory.Beverages),
        (["spread", "condiment", "sauce", "spice", "legume", "pulse", "sugar", "flour", "canned", "snack"], ProductCategory.PantryAndDryGoods),
        (["frozen"], ProductCategory.Frozen),
    ];

    // Leading mass at the start of a free-text quantity (e.g. "400 g", "1,5 kg"). Only g/kg
    // are mapped — the grams model has no place for volumes (ml/cl/l), which fall through to null.
    private static readonly Regex QuantityMassPattern = new(
        @"^\s*(?<num>\d+(?:[.,]\d+)?)\s*(?<unit>kg|g)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        // Crowdsourced data occasionally encodes numbers as strings; tolerate it.
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private readonly HttpClient _httpClient;

    public OpenFoodFactsBarcodeLookup(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BarcodeProductData?> LookupAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        // Pass the typed digits straight through — OFF normalizes (pads EAN/UPC) server-side.
        string requestUri =
            $"api/v2/product/{Uri.EscapeDataString(barcode)}?fields={Fields}&lc=pl,en";

        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken);

            // 404 (not found) and 302 (barcode belongs to a sibling OFF project) are misses,
            // not errors — fall straight through to manual entry.
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            OpenFoodFactsEnvelope? envelope = await response.Content
                .ReadFromJsonAsync<OpenFoodFactsEnvelope>(SerializerOptions, cancellationToken);

            // Branch on the body status, not solely the HTTP code (off-api-reference §5).
            if (envelope is not { Status: 1, Product: { } product })
            {
                return null;
            }

            return Map(product);
        }
        catch (Exception ex) when (
            ex is HttpRequestException or TaskCanceledException or OperationCanceledException or JsonException)
        {
            // Timeout, connection failure, or unparseable body — treat every one as a miss.
            return null;
        }
    }

    private static BarcodeProductData Map(OpenFoodFactsProduct product)
    {
        OpenFoodFactsNutriments? nutriments = product.Nutriments;

        return new BarcodeProductData(
            Name: Trimmed(product.ProductNamePl) ?? Trimmed(product.ProductName),
            Brand: Trimmed(product.Brands),
            Calories: ResolveCalories(nutriments),
            Protein: nutriments?.ProteinsPer100g,
            Fat: nutriments?.FatPer100g,
            Carbohydrates: nutriments?.CarbohydratesPer100g,
            PackageSizeGrams: ResolvePackageSizeGrams(product),
            Category: ResolveCategory(product),
            SaturatedFat: nutriments?.SaturatedFatPer100g,
            MonounsaturatedFat: nutriments?.MonounsaturatedFatPer100g,
            PolyunsaturatedFat: nutriments?.PolyunsaturatedFatPer100g,
            TransFat: nutriments?.TransFatPer100g,
            Sugars: nutriments?.SugarsPer100g,
            Fiber: nutriments?.FiberPer100g,
            Salt: nutriments?.SaltPer100g,
            Sodium: nutriments?.SodiumPer100g,
            Potassium: nutriments?.PotassiumPer100g,
            Calcium: nutriments?.CalciumPer100g,
            Iron: nutriments?.IronPer100g,
            VitaminA: nutriments?.VitaminAPer100g,
            VitaminC: nutriments?.VitaminCPer100g,
            VitaminD: nutriments?.VitaminDPer100g);
    }

    private static ProductCategory? ResolveCategory(OpenFoodFactsProduct product)
    {
        if (product.CategoriesTags is not { Count: > 0 } tags)
        {
            return null;
        }

        // Rule order is the precedence; within a rule, any tag containing any keyword matches.
        foreach ((string[] keywords, ProductCategory category) in CategoryRules)
        {
            foreach (string? tag in tags)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    continue;
                }

                foreach (string keyword in keywords)
                {
                    if (tag.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        return category;
                    }
                }
            }
        }

        return null;
    }

    private static decimal? ResolvePackageSizeGrams(OpenFoodFactsProduct product)
    {
        // Prefer the numeric product_quantity (already grams); otherwise parse the free-text
        // quantity, but only when it is a mass — volumes are out of scope for the grams model.
        if (product.ProductQuantity is { } grams && grams > 0m)
        {
            return grams;
        }

        return ParseMassGrams(product.Quantity);
    }

    private static decimal? ParseMassGrams(string? quantity)
    {
        if (string.IsNullOrWhiteSpace(quantity))
        {
            return null;
        }

        Match match = QuantityMassPattern.Match(quantity);
        if (!match.Success)
        {
            return null;
        }

        string number = match.Groups["num"].Value.Replace(',', '.');
        if (!decimal.TryParse(number, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value)
            || value <= 0m)
        {
            return null;
        }

        bool isKilograms = match.Groups["unit"].Value.Equals("kg", StringComparison.OrdinalIgnoreCase);
        return isKilograms ? value * 1000m : value;
    }

    private static decimal? ResolveCalories(OpenFoodFactsNutriments? nutriments)
    {
        if (nutriments is null)
        {
            return null;
        }

        if (nutriments.EnergyKcalPer100g is { } kcal)
        {
            return kcal;
        }

        // Energy fallback: derive kcal from kJ when only the kJ key is present.
        return nutriments.EnergyKjPer100g is { } kj
            ? kj / KilojoulesPerKilocalorie
            : null;
    }

    private static string? Trimmed(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
