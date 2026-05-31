namespace Jadlify.Infrastructure.OpenFoodFacts;

/// <summary>
/// Non-secret configuration for the Open Food Facts barcode lookup. Reads require no
/// token — only a descriptive <see cref="UserAgent"/> — so every value here is safe to
/// keep in <c>appsettings.json</c>. Bound from the <see cref="SectionName"/> section.
/// </summary>
public sealed class OpenFoodFactsOptions
{
    public const string SectionName = "OpenFoodFacts";

    /// <summary>Production base. Tests may point this at staging or an unreachable host.</summary>
    public string BaseUrl { get; init; } = "https://world.openfoodfacts.org";

    /// <summary>
    /// Descriptive User-Agent (OFF bans anonymous bot-like traffic). Format
    /// <c>AppName/Version (ContactEmail)</c> per the OFF API guidance.
    /// </summary>
    public string UserAgent { get; init; } = "Jadlify/0.1 (contact@jadlify.example)";

    /// <summary>
    /// Short request timeout. A slow or missing OFF must never stall product creation,
    /// so this is deliberately small (FR-006 / NFR "Odporność uzupełniania").
    /// </summary>
    public int TimeoutSeconds { get; init; } = 4;
}
