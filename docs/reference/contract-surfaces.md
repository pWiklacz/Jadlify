# Contract Surfaces

This registry records names that future changes must reuse when building user-owned data flows. Do not create parallel identity or authorization contracts unless this document is updated with the replacement decision.

## Account Boundary

- `ApplicationUserId` (`src/Jadlify.Application/Identity/ApplicationUserId.cs`) is the application-layer value object for the stable Supabase user subject. It accepts any non-empty subject string and compares by value.
- `ICurrentUser` (`src/Jadlify.Application/Identity/ICurrentUser.cs`) is the only application-layer abstraction for the authenticated user. Handlers and repositories should depend on this contract instead of ASP.NET Core, Supabase SDK types, or raw claims.
- `HttpContextCurrentUser` (`src/Jadlify.API/Authentication/HttpContextCurrentUser.cs`) adapts `HttpContext.User` to `ICurrentUser` in the API layer. It reads the literal `sub` claim and fails explicitly when authentication or subject identity is missing.
- `SupabaseJwtOptions` (`src/Jadlify.API/Authentication/SupabaseJwtOptions.cs`) owns non-secret JWT validation setting names. Real issuer, metadata, audience overrides, keys, and connection strings stay in user secrets or hosting configuration.
- `UserScope` (`src/Jadlify.Application/Identity/UserScope.cs`) is the reusable same-user guard for future user-owned resources. Cross-user denial returns `Result` or `Result<T>` with `ErrorType.Forbidden` and `UserScope.Forbidden`.

## Handoff Rules

- Supabase Auth access tokens map their `sub` claim to `ApplicationUserId`; inbound claim remapping stays disabled so code reads `sub` literally.
- ASP.NET Core API endpoints are the backend boundary for domain data. The browser may use Supabase for auth/session, but product, recipe, plan, goal, and shopping-list data must go through the API.
- `/health` is the only intentionally anonymous runtime endpoint. Future endpoints must rely on the fallback authenticated policy or state an explicit exception in their change plan.
- User-owned persistence in F-02 must store an owner `ApplicationUserId` value or equivalent persisted subject and pass it through `UserScope` before returning data.
