using System.IdentityModel.Tokens.Jwt;
using Jadlify.API.Authentication;
using Jadlify.API.Session;
using Jadlify.Application;
using Jadlify.Application.Identity;
using Jadlify.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<SupabaseJwtOptions>(
    builder.Configuration.GetSection(SupabaseJwtOptions.SectionName));
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

SupabaseJwtOptions supabaseJwtOptions = builder.Configuration
    .GetSection(SupabaseJwtOptions.SectionName)
    .Get<SupabaseJwtOptions>() ?? new SupabaseJwtOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Configure JwtBearer from SupabaseJwtOptions resolved through DI (bound lazily) rather
// than from an eager configuration snapshot, so configuration applied after the host is
// built — e.g. a test host's in-memory overrides — is honoured before the options bind.
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<SupabaseJwtOptions>>((options, supabaseOptionsAccessor) =>
    {
        SupabaseJwtOptions supabase = supabaseOptionsAccessor.Value;
        options.MapInboundClaims = false;

        // Supabase signs user access tokens asymmetrically (ES256) and publishes the public
        // key via JWKS/OIDC discovery under Authority. Production discovers over HTTPS; a local
        // Supabase CLI stack serves the same endpoints over HTTP, so RequireHttpsMetadata is
        // configurable (defaults to true — only local dev sets it false). Issuer/audience/
        // lifetime/sub name claim are identical across environments.
        options.Authority = NullIfWhiteSpace(supabase.Authority);
        string? metadataAddress = NullIfWhiteSpace(supabase.MetadataAddress);
        if (metadataAddress is not null)
        {
            options.MetadataAddress = metadataAddress;
        }

        options.RequireHttpsMetadata = supabase.RequireHttpsMetadata;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidIssuer = supabase.Issuer ?? string.Empty,
            ValidateAudience = true,
            ValidAudience = supabase.Audience ?? string.Empty,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            NameClaimType = supabase.RequiredSubjectClaimName,
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireClaim(supabaseJwtOptions.RequiredSubjectClaimName)
        .Build());

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Serve the built SPA from wwwroot. UseStaticFiles runs before authentication so
// static assets are returned without hitting the global "must be authenticated"
// fallback policy.
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok()).AllowAnonymous();

// Authenticated session probe: returns the caller's stable Supabase subject so the
// SPA can confirm its bearer token reaches the backend. No persistence — it reads
// ICurrentUser. Deliberately NOT AllowAnonymous, so it inherits the global fallback
// policy (authenticated + 'sub' claim). Lives under /api so the SPA fallback below
// does not shadow it.
app.MapGet("/api/me", (ICurrentUser currentUser) =>
    Results.Ok(new MeResponse(currentUser.UserId.Value)));

// Client-side routing: serve index.html for non-API deep links. Must be
// AllowAnonymous, otherwise the global fallback policy 401s the SPA entrypoint
// and anonymous users can never reach the (future) login screen. API and health
// endpoints match first, so this only catches unrouted paths.
app.MapFallbackToFile("index.html").AllowAnonymous();

app.Run();

static string? NullIfWhiteSpace(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value;

public partial class Program;
