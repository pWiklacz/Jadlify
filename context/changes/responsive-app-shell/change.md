---
change_id: responsive-app-shell
title: Responsive app shell
status: implementing
created: 2026-05-29
updated: 2026-05-29
archived_at: null
---

## Notes

<!-- Free-form notes for this change: links, ad-hoc context, decisions that don't belong in research/frame/plan. -->

- 2026-05-29: Appended **Phase 5 (Local Supabase Dev Environment + Backend JWT Wiring)** to the plan (per user decision) instead of opening a separate dev-infra change. Phase 5 provisions a port-shifted local Supabase stack (`project_id=jadlify`, ports `544xx`) that coexists with another project's local stack, and wires the API to validate the tokens it issues. It unblocks the manual auth round-trip checks deferred from Phases 2–3 (2.3/2.4, 3.4–3.6). Phase 5 has no hard ordering dependency on Phase 4's code; it can be implemented before Phase 4's manual verification.
- 2026-05-29: **Phase 5 implementation pivot.** The plan assumed local GoTrue signs **HS256** tokens with a shared secret, but the installed Supabase CLI (v2.84.2) issues **ES256** (asymmetric, JWKS) user tokens — confirmed live (`/api/me` returned `401` with the symmetric path; the token header was `alg: ES256`). Per a decision-during-implementation, dropped the symmetric `SigningKey` path and instead made `SupabaseJwtOptions.RequireHttpsMetadata` configurable so the existing asymmetric JWKS path validates the local stack over HTTP (`Authority=http://127.0.0.1:54421/auth/v1`, `RequireHttpsMetadata=false` in dev; production keeps HTTPS). The 5.2 test was rewritten from HS256 to in-process ES256. Live round-trip now passes: anonymous→`401`, valid ES256 token→`200`+`sub`, refreshed token→`200` (5.6/5.7 confirmed, unblocking 2.3/2.4 and 3.6).
