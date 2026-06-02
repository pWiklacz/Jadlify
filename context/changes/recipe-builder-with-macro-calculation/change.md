---
change_id: recipe-builder-with-macro-calculation
title: Recipe builder with macro calculation
status: impl_reviewed
created: 2026-05-31
updated: 2026-06-02
archived_at: null
---

## Notes

<!-- Free-form notes for this change: links, ad-hoc context, decisions that don't belong in research/frame/plan. -->

### Support changes outside the plan's file list (impl review, 2026-06-02)

These benign, necessary changes were made beyond the plan's enumerated files to keep the test host and build green:

- `tests/Jadlify.API.Tests/Common/TestApiFactory.cs` — `UseWebRoot(AppContext.BaseDirectory)` + logging trim so `WebApplicationFactory` doesn't fail on the missing SPA `wwwroot`.
- `tests/Jadlify.API.Tests/Authentication/*.cs`, `tests/Jadlify.API.Tests/Session/SessionEndpointTests.cs` — adjusted accordingly.
- `.scripts/build-min.ps1` — MSBuild parallelism flags.
- `src/Jadlify.Infrastructure/Persistence/JadlifyDbContextFactory.cs` — `using` reorder only (no behavioral change).
