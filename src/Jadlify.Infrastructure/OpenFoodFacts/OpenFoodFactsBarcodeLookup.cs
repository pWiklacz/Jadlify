using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jadlify.Application.Products;

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
    private const string Fields = "product_name,product_name_pl,brands,quantity,nutriments";

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
            Carbohydrates: nutriments?.CarbohydratesPer100g);
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
