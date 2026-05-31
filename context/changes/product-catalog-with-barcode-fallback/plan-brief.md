# Product Catalog With Barcode Fallback (S-02) — Plan Brief

> Full plan: `context/changes/product-catalog-with-barcode-fallback/plan.md`
> Research: `context/changes/product-catalog-with-barcode-fallback/research-barcode-api.md`, `off-api-reference.md`

## What & Why

Let a signed-in user **add, review, edit, and delete their own products**, where typing a barcode pre-fills the product form from the Open Food Facts (OFF) API and **manual entry is always the fallback** (FR-003…FR-006, US-02). Barcode is a helper, never a blocker — a miss or an OFF outage must still let the user save manually.

## Starting Point

The `Product` data layer already exists end-to-end: domain entity (name, optional barcode, owned per-100g `MacroNutrients`), EF configuration (`products` table, `(user_id, barcode)` index), and a fully implemented owner-scoped `ProductRepository`. A CQRS mediator + FluentValidation pipeline exists but has **no product handlers yet** (S-02 is the first vertical). The API is a Minimal API with a global auth fallback and only `/health` + `/api/me`. The frontend has auth/session done (S-01) and a `ProductsPage` placeholder; its `ApiClient` only supports `get`.

## Desired End State

`/products` shows the user's catalog. **Add product** opens a modal that is also the edit form and hosts a barcode **Look up** button: a hit pre-fills, partial data pre-fills what exists, a miss/outage keeps the typed barcode on an otherwise blank form, and a barcode already in the catalog offers the existing product to edit. Save/edit/delete (with confirmation) all work, scoped strictly to the signed-in user.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) | Source |
| --- | --- | --- | --- |
| Barcode API | Open Food Facts v2, server-side, snapshot at add-time | Free, no key, EU/PL-strong, non-blocking `status` branching | Research |
| Field mapping on partial data | Pre-fill what exists; derive kcal from kJ÷4.184 when kcal absent | Maximizes hit rate, matches US-02 partial-data rule | Research |
| Delete of a used product | **Keep historical** — S-02 removes the existing in-use block | Chosen policy; no recipes exist yet so it's safe in S-02 | Plan |
| Realising "keep historical" | Drop `Product.InUse` guard now; **S-03 must snapshot macros into `RecipeIngredient`** | Aligns code with policy without over-reaching into S-03 | Plan |
| Duplicate barcode already saved | Surface the existing product, offer to edit | Uses existing `GetByBarcodeAsync`, avoids duplicates | Plan |
| Add/edit + barcode UX | Modal over the list, single form for manual + lookup | Keeps list context, no extra routing, matches US-02 | Plan |
| OFF resilience | Production base, ~4s timeout, no cache, any failure → manual | Rate limit irrelevant at single-user scale; minimal code | Plan |
| Validation | Minimum + sane physical bounds (name ≤200; macros ≥0 & ≤100/100g; kcal ≤~900) | Catches typos without rejecting legitimate data | Plan |

## Scope

**In scope:** Product CRUD use-cases (CQRS) + validators; `IBarcodeProductLookup` port + OFF adapter; `/api/products` endpoints + `Result`→HTTP mapping; Products frontend (list, modal form, barcode pre-fill, delete confirm); unit/integration/component tests; removal of the in-use delete block.

**Out of scope:** camera scanner, OFF write-back/search, snapshot refresh over time, non-gram units, OFF caching, list search/sort/pagination, recipe-side macro snapshot (S-03), and all later slices (S-03…S-06). No schema change / EF migration.

## Architecture / Approach

Bottom-up vertical slice on the existing data layer. Application owns the `IBarcodeProductLookup` port (mirroring `IProductRepository`); Infrastructure implements it as a thin, swappable OFF adapter (typed `HttpClient`). Flow: SPA modal → `GET /api/products/barcode/{code}` → `LookupBarcodeQuery` (own catalog first, then OFF) → pre-filled form → `POST/PUT /api/products` → mediator → repository → Postgres. Failures collapse to a non-blocking "not found" so manual entry always works.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Application | Product commands/queries/handlers/validators + barcode port | First CQRS vertical — sets the pattern others copy |
| 2. Infrastructure | OFF adapter (mapping + resilience); remove in-use delete block | OFF data variability; correctly swallowing every failure mode |
| 3. API | `/api/products` endpoints + `Result`→HTTP mapping; integration tests | Test host must swap Npgsql→SQLite + stub lookup; enforce per-user isolation |
| 4. Frontend | Products page: list + modal form + barcode pre-fill + delete | Four-way barcode UX branching; modal accessibility |

**Prerequisites:** S-01 (auth/session) and F-02 (data layer) — both already in place.
**Estimated effort:** ~3–4 focused sessions, one per phase.

## Open Risks & Assumptions

- "Keep historical" only fully holds once S-03 snapshots product macros into `RecipeIngredient`; until then deletion is unconstrained, which is safe only because no recipe feature exists yet.
- OFF is crowdsourced and rate-limited; the adapter must treat every error as a non-blocking miss, or product creation could stall (NFR violation).
- Assumes the app-wide `QueryClientProvider` from S-01 is available for the new react-query hooks.

## Success Criteria (Summary)

- A user adds products both manually and via barcode (all four outcomes), then edits and deletes them, entirely within `/products`.
- A barcode miss or OFF outage never blocks manual product creation.
- One user can never read or mutate another user's products (verified by an integration test).
