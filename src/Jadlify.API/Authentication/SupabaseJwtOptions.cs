namespace Jadlify.API.Authentication;

public sealed class SupabaseJwtOptions
{
    public const string SectionName = "SupabaseAuth";

    public string? Authority { get; init; }

    public string? Issuer { get; init; }

    public string? Audience { get; init; } = "authenticated";

    public string? MetadataAddress { get; init; }

    public string RequiredSubjectClaimName { get; init; } = "sub";
}
