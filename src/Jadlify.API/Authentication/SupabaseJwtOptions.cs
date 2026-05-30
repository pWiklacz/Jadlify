namespace Jadlify.API.Authentication;

public sealed class SupabaseJwtOptions
{
    public const string SectionName = "SupabaseAuth";

    public string? Authority { get; init; }

    public string? Issuer { get; init; }

    public string? Audience { get; init; } = "authenticated";

    public string? MetadataAddress { get; init; }

    /// <summary>
    /// Whether OIDC/JWKS metadata discovery must use HTTPS. Defaults to <c>true</c> (production).
    /// Set to <c>false</c> only for local development against a Supabase CLI stack, whose JWKS /
    /// discovery endpoints are served over HTTP. Supabase signs user access tokens asymmetrically
    /// (ES256) in both cases; only the metadata transport differs.
    /// </summary>
    public bool RequireHttpsMetadata { get; init; } = true;

    public string RequiredSubjectClaimName { get; init; } = "sub";
}
