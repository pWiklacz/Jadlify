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

- 2026-05-29: Appended **Phase 5 (Local Supabase Dev Environment + Backend JWT Wiring)** to the plan (per user decision) instead of opening a separate dev-infra change. Phase 5 provisions a port-shifted local Supabase stack (`project_id=jadlify`, ports `544xx`) that coexists with another project's local stack, and adds a Development symmetric-key (HS256) JWT validation path to the API. It unblocks the manual auth round-trip checks deferred from Phases 2–3 (2.3/2.4, 3.4–3.6). Phase 5 has no hard ordering dependency on Phase 4's code; it can be implemented before Phase 4's manual verification.
