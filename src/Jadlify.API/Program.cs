using System.IdentityModel.Tokens.Jwt;
using Jadlify.API.Authentication;
using Jadlify.Application;
using Jadlify.Application.Identity;
using Jadlify.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.Authority = NullIfWhiteSpace(supabaseJwtOptions.Authority);
        string? metadataAddress = NullIfWhiteSpace(supabaseJwtOptions.MetadataAddress);
        if (metadataAddress is not null)
        {
            options.MetadataAddress = metadataAddress;
        }

        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidIssuer = supabaseJwtOptions.Issuer ?? string.Empty,
            ValidateAudience = true,
            ValidAudience = supabaseJwtOptions.Audience ?? string.Empty,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            NameClaimType = supabaseJwtOptions.RequiredSubjectClaimName,
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

// Client-side routing: serve index.html for non-API deep links. Must be
// AllowAnonymous, otherwise the global fallback policy 401s the SPA entrypoint
// and anonymous users can never reach the (future) login screen. API and health
// endpoints match first, so this only catches unrouted paths.
app.MapFallbackToFile("index.html").AllowAnonymous();

app.Run();

static string? NullIfWhiteSpace(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value;

public partial class Program;
