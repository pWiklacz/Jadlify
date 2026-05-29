namespace Jadlify.API.Session;

/// <summary>
/// Response body for <c>GET /api/me</c>: the authenticated caller's stable Supabase
/// subject (<see cref="Jadlify.Application.Identity.ApplicationUserId"/> value).
/// </summary>
public sealed record MeResponse(string UserId);
