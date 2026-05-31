# Product Catalog With Barcode Fallback (S-02) Implementation Plan

## Overview

Deliver the S-02 vertical slice: a signed-in user can **add, review, edit, and delete their own products**, where entering a barcode pre-fills the product form from the Open Food Facts (OFF) API and **manual entry is always available as a fallback** (FR-003…FR-006, US-02).

The data layer for `Product` already exists end-to-end (entity, owned `MacroNutrients`, EF configuration, fully implemented `ProductRepository` with owner scoping and a barcode lookup). This plan adds the four layers that are still missing on top of it: **Application use-cases (CQRS) + a barcode-lookup port**, an **OFF HTTP adapter**, **API endpoints with a `Result`→HTTP mapping**, and the **Products frontend** (list + modal form with barcode pre-fill).

## Current State Analysis

What already exists and this plan builds on:

- **Domain** — `Product` (`src/Jadlify.Domain/Products/Product.cs`) holds `Id`, `Name`, optional `Barcode`, and an owned `MacroNutrients` (`Per100Grams`). `MacroNutrients` (per-100g kcal/protein/fat/carbs) rejects negatives and supports `Scale`/`+`. Grams are the only unit (PRD Open Q#2 / FR-003).
- **Application** — owner-scoped `IProductRepository` (`Add`, `GetById`, `List`, `ListByIds`, `GetByBarcode`, `Update`, `Delete`). A hand-rolled CQRS mediator (`IMediator.SendAsync`/`QueryAsync`, `ICommand`/`ICommand<T>`/`IQuery<T>`, `ICommandHandler`/`IQueryHandler`) with FluentValidation via `ValidationBehavior`. Handlers/validators are auto-registered by assembly scan (`DependencyInjection.AddCQRS`). **No product handlers exist yet — S-02 is the first CQRS vertical.**
- **Infrastructure** — `ProductRepository` is fully implemented against `JadlifyDbContext` (Npgsql/Supabase Postgres), stamping the shadow `UserId` and scoping every query. `ProductConfiguration` maps the `products` table (owned macros, `(user_id, barcode)` index). **No outbound `HttpClient` exists yet.**
- **API** — `Program.cs` is a Minimal API with a global fallback auth policy (authenticated + `sub` claim). Only `/health`, `/api/me`, and the SPA fallback are mapped. **No product endpoints and no `Result`→HTTP mapping helper exist yet.**
- **Frontend** (`src/Jadlify.Web`, React 19 + react-router 7 + @tanstack/react-query + Tailwind) — `ApiClient` exposes **only `get<T>`** (`src/api/client.ts`); `apiClient` attaches the live Supabase bearer token. `useMe` is the react-query data-hook pattern. `ProductsPage` is a `SectionPlaceholder` wired into the protected `/products` route. Auth/session (`RequireAuth`, `SessionProvider`, `useSession`) is complete (S-01 `impl_reviewed`).
- **Tests** — repository tests use `SqliteTestDatabase` (in-memory SQLite, `EnsureCreated`) + `TestCurrentUser`. API tests use `WebApplicationFactory<Program>` + a `TestAuthenticationHandler` where the bearer token *is* the `sub` claim.

### Key Discoveries:

- The `Product` data layer is complete; **S-02 introduces no schema change and no EF migration** (`ProductConfiguration` already defines barcode + owned macros + indexes).
- `ProductRepository.DeleteAsync` (`src/Jadlify.Infrastructure/Persistence/Repositories/ProductRepository.cs:110`) currently **blocks deletion** when the product is referenced by a recipe (`Error.Conflict("Product.InUse")`). The chosen policy is **"keep historical"**, which is the *opposite* — S-02 removes this block (see Critical Implementation Details).
- The block is asserted by `ProductRepositoryTests.DeleteAsync_ReturnsConflict_WhenProductUsedByRecipe` (`tests/Jadlify.Infrastructure.Tests/Persistence/ProductRepositoryTests.cs:114`) — this test must be rewritten.
- `RecipeIngredient` (`src/Jadlify.Domain/Recipes/RecipeIngredient.cs`) stores only `ProductId` + `WholeRecipeAmount`. "Keep historical" only fully holds once S-03 snapshots product macros into the ingredient — recorded here as a cross-slice contract.
- OFF integration mechanics are settled in `off-api-reference.md` (envelope, `status` branching, `nutriments.*_100g` keys, field mapping §8) and `research-barcode-api.md`. No re-research needed.
- API integration tests for product endpoints must swap the Npgsql `JadlifyDbContext` for SQLite in-memory and stub `IBarcodeProductLookup`, because the existing `AuthBoundaryTests` factory never touches the DB or the network.

## Desired End State

A signed-in user opens `/products` and can:

1. See a list of their own products (name, barcode, per-100g kcal/protein/fat/carbs).
2. Click **Add product**, open a modal, and either type values manually or enter a barcode and click **Look up** to pre-fill the form from OFF; missing fields stay blank for manual completion; a not-found / OFF-outage leaves an empty form with the barcode kept; if the barcode is already in their catalog they are offered the existing product to edit.
3. Save, edit, and delete their products, with delete behind a confirmation.

Verification: all automated checks in each phase pass, and the manual end-to-end walkthrough (manual add, the four barcode outcomes, edit, delete) succeeds in the browser against a real Supabase session.

## What We're NOT Doing

- **Camera barcode scanner** — barcode is typed (PRD Non-Goals); FR-004 is manual entry of the code.
- **Writing data back to OFF** — read-only adapter (off-api-reference §11).
- **OFF full-text/category search** — only get-by-barcode; the `/search` endpoint is out of scope.
- **Refreshing snapshot data over time** — values are frozen at add-time (PRD Open Q#3 resolved); the user edits afterward.
- **Units other than grams** (PRD Open Q#2), calorie↔macro coherence validation, response caching of OFF lookups, and list search/sort/pagination — all deferred (single-user MVP; NFR limits are far off).
- **Recipe-side macro snapshot** — that is the S-03 obligation created by the "keep historical" delete policy (named below), not built here.
- **Recipes, goals, meal plan, shopping list** — later slices (S-03…S-06).

## Implementation Approach

Build bottom-up so each phase is independently testable: Application use-cases (pure, fake-backed unit tests) → Infrastructure OFF adapter (stubbed `HttpMessageHandler`) → API endpoints (integration tests with SQLite + stubbed lookup + test auth) → Frontend (Vitest/RTL). Ports live in Application (`IBarcodeProductLookup`, mirroring how `IProductRepository` is owned by Application and implemented in Infrastructure); the OFF adapter is a thin, swappable implementation behind that port (research "thin adapter" guidance — enables a later FatSecret swap without touching the rest of the slice).

## Critical Implementation Details

- **Delete policy = "keep historical"; S-02 removes the in-use block.** `ProductRepository.DeleteAsync` must drop the `_context.Recipes` in-use check and the `Product.InUse` conflict, so deletion always succeeds for an owned product (still 404 for another user's product). This is safe in S-02 because no recipe feature exists yet. **Cross-slice contract for S-03:** `RecipeIngredient` must snapshot the product's name + per-100g macros at add-time, so a recipe keeps correct totals after its product is deleted. Record this in the S-03 change notes when that slice is planned.
- **Barcode lookup must never block product creation** (FR-006 / NFR "Odporność uzupełniania"). The OFF adapter swallows *every* failure mode — HTTP timeout, non-2xx, `404`/`302`, malformed JSON, and body `status: 0` — into a single "not found / no data" result and never throws to the handler or endpoint. Branch on the response **body `status`**, not solely the HTTP status code (off-api-reference §5).
- **Energy fallback:** when `nutriments.energy-kcal_100g` is absent but `energy-kj_100g` is present, derive kcal as `kJ ÷ 4.184`; otherwise leave kcal blank. Map only the per-100g (`*_100g`) keys; never the ambiguous base keys (off-api-reference §7–§8).
- **No client-side barcode normalization** — pass typed digits straight through to OFF (it pads EAN/UPC server-side).
- **API integration test host** must, in `ConfigureTestServices`, replace the Npgsql `JadlifyDbContext` registration with an open SQLite in-memory connection (pattern from `SqliteTestDatabase`) and replace `IBarcodeProductLookup` with a controllable stub, layered on top of the existing `TestAuthenticationHandler`.

---

## Phase 1: Application — Product Use-Cases & Barcode Port

### Overview

Add the CQRS commands, queries, DTOs, validators, and handlers for product CRUD, plus the `IBarcodeProductLookup` port and a `LookupBarcodeQuery` that checks the user's own catalog before falling through to OFF. Pure application logic, unit-tested with fakes — no HTTP, no DB.

### Changes Required:

#### 1. Product DTOs & barcode-lookup contracts

**File**: `src/Jadlify.Application/Products/ProductDto.cs`, `src/Jadlify.Application/Products/BarcodeLookupResult.cs`

**Intent**: Define the data shapes the handlers return: a `ProductDto` (id, name, barcode, four per-100g macro decimals) and a `BarcodeLookupResult` carrying an outcome plus optional pre-fill fields.

**Contract**: `ProductDto` — `Guid Id, string Name, string? Barcode, decimal Calories, decimal Protein, decimal Fat, decimal Carbohydrates`. `BarcodeLookupResult` — `enum BarcodeLookupOutcome { Found, NotFound, AlreadyInCatalog }`, plus `string Barcode`, `Guid? ExistingProductId`, and **nullable** `string? Name`, `string? Brand`, `decimal? Calories/Protein/Fat/Carbohydrates` (nullable so partial OFF data maps to blank form fields).

#### 2. Barcode-lookup port

**File**: `src/Jadlify.Application/Products/IBarcodeProductLookup.cs`

**Intent**: Application-owned port for fetching product data by barcode from an external source; Infrastructure implements it (Phase 2).

**Contract**: `Task<BarcodeProductData?> LookupAsync(string barcode, CancellationToken ct = default)` where `BarcodeProductData` is a small record of optional fields (`Name`, `Brand`, `Calories`, `Protein`, `Fat`, `Carbohydrates`, all nullable). `null` return = "no data / not found". The port never throws for a miss or an outage.

#### 3. Commands + handlers

**File**: `src/Jadlify.Application/Products/CreateProduct/*`, `UpdateProduct/*`, `DeleteProduct/*`

**Intent**: One folder per use-case (command + handler), mirroring the mediator contracts. Create builds `MacroNutrients` + `Product` and returns the new id; Update reconstructs a `Product` and forwards the repository's `Result`; Delete forwards the repository's `Result`.

**Contract**:
- `CreateProductCommand : ICommand<Guid>` — `Name, Barcode?, Calories, Protein, Fat, Carbohydrates`. Handler: `repo.AddAsync(new Product(Guid.NewGuid(), name, new MacroNutrients(...), barcode))` → `Result.Ok(id)`.
- `UpdateProductCommand : ICommand` — `Id, Name, Barcode?, Calories, Protein, Fat, Carbohydrates`. Handler: `repo.UpdateAsync(new Product(id, ...))` → return repo `Result` (NotFound propagates).
- `DeleteProductCommand : ICommand` — `Id`. Handler: `repo.DeleteAsync(id)` → return repo `Result`.

#### 4. Queries + handlers

**File**: `src/Jadlify.Application/Products/ListProducts/*`, `GetProduct/*`, `LookupBarcode/*`

**Intent**: List/Get map repository entities to `ProductDto`. `LookupBarcodeQuery` implements the dedupe-first rule: check the user's catalog, then OFF.

**Contract**:
- `ListProductsQuery : IQuery<IReadOnlyList<ProductDto>>` → `repo.ListAsync()` mapped.
- `GetProductQuery(Guid Id) : IQuery<ProductDto>` → `repo.GetByIdAsync` → `ProductDto` or `Result.Fail(NotFound)`.
- `LookupBarcodeQuery(string Barcode) : IQuery<BarcodeLookupResult>`. Handler order: (1) `repo.GetByBarcodeAsync(barcode)` — if found, return `AlreadyInCatalog` with `ExistingProductId` and that product's fields; (2) else `IBarcodeProductLookup.LookupAsync` — non-null → `Found` with mapped fields, null → `NotFound` (barcode echoed). Always a success `Result` — not-found is a normal outcome, not an error.

#### 5. Validators

**File**: `src/Jadlify.Application/Products/CreateProduct/CreateProductCommandValidator.cs`, `UpdateProduct/UpdateProductCommandValidator.cs`

**Intent**: Minimal-plus-sane-bounds validation (chosen policy): name required and bounded; macros within physically possible per-100g ranges; barcode optional and lightly shaped. No calorie↔macro coherence check.

**Contract**: `Name` not empty, ≤ 200 chars. `Protein`/`Fat`/`Carbohydrates` ≥ 0 and ≤ 100 (g per 100g). `Calories` ≥ 0 and ≤ a named `MaxCaloriesPer100g` constant (≈ 900). `Barcode` (when present) digits-only, length 8–14, not normalized. Bound constants live next to the validators so the same limits are reusable.

### Success Criteria:

#### Automated Verification:

- [ ] Build passes: `pwsh ./.scripts/build-min.ps1 -Project src/Jadlify.Application`
- [ ] Application unit tests pass (handlers with fake `IProductRepository`/`IBarcodeProductLookup`, validators): `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.Application.Tests`
- [ ] Format check passes: `pwsh ./.scripts/format-min.ps1 -Project src/Jadlify.Application`

#### Manual Verification:

- [ ] `LookupBarcodeQuery` resolves own-catalog hit before calling OFF (dedupe decision), confirmed by reading the handler.
- [ ] Validators reject impossible values (e.g. 9000 kcal/100g, 150 g protein/100g) but accept realistic OFF/manual data.

**Implementation Note**: After completing this phase and all automated verification passes, pause for manual confirmation before proceeding.

---

## Phase 2: Infrastructure — Open Food Facts Adapter & Delete-Policy Alignment

### Overview

Implement `IBarcodeProductLookup` against OFF v2 via a typed `HttpClient`, with full non-blocking resilience and the §8 field mapping (incl. kJ→kcal fallback). Align `ProductRepository.DeleteAsync` with the "keep historical" policy by removing the in-use block, and update the affected test.

### Changes Required:

#### 1. OFF options + typed HTTP adapter

**File**: `src/Jadlify.Infrastructure/OpenFoodFacts/OpenFoodFactsOptions.cs`, `OpenFoodFactsBarcodeLookup.cs`, plus internal response DTOs

**Intent**: Call OFF get-by-barcode, parse the envelope, branch on body `status`, map per-100g nutriments to `BarcodeProductData`, and convert every failure into a `null` (not-found) return so the lookup is never a blocker.

**Contract**:
- `OpenFoodFactsOptions` — `SectionName = "OpenFoodFacts"`, `BaseUrl` (default `https://world.openfoodfacts.org`), `UserAgent` (e.g. `Jadlify - Web - Version 0.1 - <url>`), `TimeoutSeconds` (default ~4).
- `OpenFoodFactsBarcodeLookup : IBarcodeProductLookup` issues `GET /api/v2/product/{barcode}?fields=product_name,product_name_pl,brands,quantity,nutriments&lc=pl,en`. Deserialize envelope `{ status, product }`; `status != 1` (or `product` null) → `null`. Map: name = `product_name_pl ?? product_name`; brand = `brands`; macros from `nutriments` per off-api-reference §8 using `*_100g` keys; kcal fallback `energy-kj_100g ÷ 4.184` when `energy-kcal_100g` missing. Wrap the whole call in try/catch over `HttpRequestException`/`TaskCanceledException`/`JsonException` → return `null`. Nutriment keys use **hyphens** (`energy-kcal`) — note the JSON property names differ from C# identifiers.

#### 2. DI registration for the adapter

**File**: `src/Jadlify.Infrastructure/DependencyInjection.cs`

**Intent**: Register the typed client with the OFF base address, `User-Agent` default header, and timeout; bind `OpenFoodFactsOptions` from configuration.

**Contract**: `services.AddHttpClient<IBarcodeProductLookup, OpenFoodFactsBarcodeLookup>(...)` configured from `OpenFoodFactsOptions`; `services.Configure<OpenFoodFactsOptions>(configuration.GetSection(OpenFoodFactsOptions.SectionName))`. Add a default `OpenFoodFacts` section to `src/Jadlify.API/appsettings.json` (non-secret: base URL, user-agent, timeout).

#### 3. Remove the in-use delete block

**File**: `src/Jadlify.Infrastructure/Persistence/Repositories/ProductRepository.cs`

**Intent**: Implement "keep historical" — deletion of an owned product always succeeds; another user's product still returns NotFound.

**Contract**: In `DeleteAsync`, drop the `_context.Recipes … AnyAsync` in-use query and the `InUse` failure path (and the now-unused `InUse` static `Error`). Keep the owner-scoped lookup + `NotFound`. Method reduces to: find owned product → NotFound if missing → `Remove` + `SaveChanges` → `Ok`.

#### 4. Update the repository delete test

**File**: `tests/Jadlify.Infrastructure.Tests/Persistence/ProductRepositoryTests.cs`

**Intent**: Replace the conflict-on-in-use assertion with the new policy.

**Contract**: Rewrite `DeleteAsync_ReturnsConflict_WhenProductUsedByRecipe` → e.g. `DeleteAsync_RemovesProduct_EvenWhenUsedByRecipe`: arrange a product referenced by a recipe, assert `result.IsSuccess` and the product is gone. Keep the other delete tests unchanged.

### Success Criteria:

#### Automated Verification:

- [ ] Build passes: `pwsh ./.scripts/build-min.ps1 -Project src/Jadlify.Infrastructure`
- [ ] Infrastructure tests pass (adapter mapping over stubbed `HttpMessageHandler` + updated delete test): `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.Infrastructure.Tests`
- [ ] Adapter unit tests cover: found+full, partial macros (blanks preserved), only-kJ (kcal derived), `status: 0`, HTTP 404/302, timeout, malformed JSON — each yielding the expected mapped value or `null`.
- [ ] Format check passes: `pwsh ./.scripts/format-min.ps1 -Project src/Jadlify.Infrastructure`

#### Manual Verification:

- [ ] Smoke against production OFF with Nutella `3017624010701` returns name + per-100g macros mapped correctly.
- [ ] Pointing `BaseUrl` at an unreachable host (or forcing a timeout) returns "not found" rather than throwing.

**Implementation Note**: Pause for manual confirmation after automated verification passes before proceeding.

---

## Phase 3: API — Product Endpoints & Result→HTTP Mapping

### Overview

Expose the use-cases over `/api/products` Minimal API endpoints, introduce a reusable `Result`→`IResult` mapping, and cover everything with integration tests (SQLite in-memory DB, stubbed lookup, test auth), including per-user isolation.

### Changes Required:

#### 1. Result→HTTP mapping helper

**File**: `src/Jadlify.API/Common/ResultExtensions.cs`

**Intent**: One place that turns a `Result`/`Result<T>` into an `IResult`, mapping `ErrorType` to status codes consistently for all current and future endpoints.

**Contract**: `Validation → 400` (emit the `ValidationError.Errors` as field messages), `NotFound → 404`, `Conflict → 409`, `Forbidden → 403`, `Problem → 500`, `Failure`/other → 400. Success is shaped by the caller (the endpoint picks 200/201/204). Use ASP.NET `Results.Problem`/`ValidationProblem` shapes; never leak tokens or internal detail (NFR "Prywatność operacyjna").

#### 2. Request/response contracts

**File**: `src/Jadlify.API/Products/ProductContracts.cs`

**Intent**: HTTP-facing records, decoupled from Application DTOs.

**Contract**: `CreateProductRequest`/`UpdateProductRequest` (name, barcode?, four macros), `ProductResponse` (mirrors `ProductDto`), `BarcodeLookupResponse` (outcome string + optional fields + `existingProductId`).

#### 3. Product endpoint module

**File**: `src/Jadlify.API/Products/ProductEndpoints.cs` (+ `app.MapProductEndpoints()` in `src/Jadlify.API/Program.cs`)

**Intent**: Map the six routes to the mediator; rely on the existing global fallback auth policy (authenticated + `sub`), so no per-route `AllowAnonymous`.

**Contract** (all under `/api/products`, all authenticated):
- `GET /` → `ListProductsQuery` → 200 `ProductResponse[]`.
- `GET /{id:guid}` → `GetProductQuery` → 200 or 404.
- `POST /` → `CreateProductCommand` → 201 + `Location: /api/products/{id}` + `ProductResponse`.
- `PUT /{id:guid}` → `UpdateProductCommand` → 204 / 404 / 400.
- `DELETE /{id:guid}` → `DeleteProductCommand` → 204 / 404.
- `GET /barcode/{barcode}` → `LookupBarcodeQuery` → **always 200** `BarcodeLookupResponse` (Found / NotFound / AlreadyInCatalog), per FR-006.

#### 4. Endpoint integration tests + test host

**File**: `tests/Jadlify.API.Tests/Products/ProductEndpointsTests.cs` (+ a shared test factory, e.g. `tests/Jadlify.API.Tests/Common/TestApiFactory.cs`)

**Intent**: Exercise the endpoints over the real pipeline with a swapped DB and stubbed lookup.

**Contract**: Factory replaces `JadlifyDbContext` with an open in-memory SQLite connection (`EnsureCreated`) and registers a stub `IBarcodeProductLookup`, on top of `TestAuthenticationHandler` (token = `sub`). Cover: create→201+Location; list/get happy paths; **user B cannot GET/PUT/DELETE user A's product (404)**; update 404; validation 400; delete 204; barcode lookup Found/NotFound/AlreadyInCatalog all 200 with expected bodies.

### Success Criteria:

#### Automated Verification:

- [ ] Build passes: `pwsh ./.scripts/build-min.ps1 -Project src/Jadlify.API`
- [ ] API integration tests pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.API.Tests`
- [ ] Cross-user isolation test (user B → user A's product → 404) passes (NFR "Izolacja danych").
- [ ] Verify script passes: `pwsh ./.scripts/verify-min.ps1 -BuildProject src/Jadlify.API -TestProject tests/Jadlify.API.Tests`

#### Manual Verification:

- [ ] With a real Supabase token (or via OpenAPI UI in Development), run create → list → get → update → delete and confirm 201/200/204/404/400 status codes.
- [ ] `GET /api/products/barcode/3017624010701` returns a populated `Found` body; an unknown code returns `NotFound` with HTTP 200.

**Implementation Note**: Pause for manual confirmation after automated verification passes before proceeding.

---

## Phase 4: Frontend — Products Page (List + Modal Form + Barcode Pre-fill)

### Overview

Replace the `ProductsPage` placeholder with a real catalog UI: a list, an **Add product** modal that doubles as the edit form and hosts the barcode lookup, and delete-with-confirmation — all wired through react-query and a mutation-capable API client.

### Changes Required:

#### 1. Extend the API client with mutations

**File**: `src/Jadlify.Web/src/api/client.ts`

**Intent**: `ApiClient` currently only does `get`; add `post`/`put`/`delete` so products can be created/updated/removed while keeping the bearer-token + `ApiError` behavior.

**Contract**: Add `post<T>(path, body): Promise<T>`, `put<T>(path, body): Promise<T>` (handles 204 → `undefined`), `del(path): Promise<void>` to the `ApiClient` interface and the `createApiClient` impl; JSON `Content-Type` on bodies. Existing `get` and token resolution unchanged. Update `src/api/client.test.ts` accordingly.

#### 2. Product types + react-query hooks

**File**: `src/Jadlify.Web/src/products/types.ts`, `useProducts.ts`, `useProductMutations.ts`, `useBarcodeLookup.ts`

**Intent**: Type the product/lookup shapes (mirror the API contracts) and provide a query for the list, mutations for create/update/delete (invalidating the `['products']` key), and an on-demand barcode lookup.

**Contract**: `useProducts()` → `useQuery(['products'], GET /api/products)`, enabled when a session exists (mirror `useMe`). `useCreateProduct`/`useUpdateProduct`/`useDeleteProduct` → `useMutation` calling `apiClient.post/put/del`, `onSuccess` invalidates `['products']`. `useBarcodeLookup()` → `useMutation` calling `GET /api/products/barcode/{barcode}`, returning the `BarcodeLookupResponse`. Reuse the existing app-wide `QueryClientProvider` (already present for `useMe`).

#### 3. Products UI components

**File**: `src/Jadlify.Web/src/products/ProductsPage.tsx` (replaces the placeholder), `ProductFormModal.tsx`, `DeleteProductDialog.tsx`; update `src/Jadlify.Web/src/routes/sections/ProductsPage.tsx` to render the new page

**Intent**: List products with Edit/Delete actions; a modal form for add/edit that contains the barcode field + **Look up** button which pre-fills the form per the four US-02 outcomes; a delete confirmation.

**Contract**:
- List: table/cards of name, barcode, per-100g kcal/protein/fat/carbs + Edit/Delete; **Add product** opens the modal in create mode; empty state when no products.
- `ProductFormModal`: controlled fields (name, barcode, 4 macros). **Look up** calls `useBarcodeLookup` and branches: `Found` → pre-fill present fields, leave missing blank (form stays fully editable); `NotFound`/error → keep barcode, blank macros, show "no data — fill manually"; `AlreadyInCatalog` → inform and offer to edit the existing product (`existingProductId`). Submit calls create or update. Accessibility: focus trap, ESC to close, labelled inputs (NFR keyboard usability).
- `DeleteProductDialog`: confirm → `useDeleteProduct`.
- Loading/feedback: show progress for in-flight lookups/saves (NFR — visible feedback ≤200ms, progress for >2s).

#### 4. Component tests

**File**: `src/Jadlify.Web/src/products/*.test.tsx`

**Intent**: Cover the list and the barcode-driven form flows with mocked API.

**Contract**: Vitest + RTL with a `QueryClientProvider` wrapper and a mocked `apiClient`/fetch: list render + empty state; manual create; barcode `Found` pre-fill; `NotFound` keeps barcode + blank form; `AlreadyInCatalog` offers edit; edit submit; delete confirm flow.

### Success Criteria:

#### Automated Verification:

- [ ] Lint passes: `npm run lint` (in `src/Jadlify.Web`)
- [ ] Frontend tests pass: `npm test` (in `src/Jadlify.Web`)
- [ ] Production build passes: `npm run build` (in `src/Jadlify.Web`)

#### Manual Verification:

- [ ] In dev (backend + Vite), manually add a product and see it in the list.
- [ ] Barcode flow: a known code pre-fills; a partial-data code pre-fills available fields only; an unknown code leaves an empty form with the barcode kept; a code already in the catalog offers the existing product to edit.
- [ ] Edit and delete (with confirmation) work and the list updates.
- [ ] Usable on a mobile-width viewport; interactions give visible feedback and long operations show progress.

**Implementation Note**: After automated verification passes, pause for manual confirmation before proceeding.

---

## Phase 5: Extended Nutrition Facts & Package Size (Richer OFF Snapshot)

### Overview

Pull and store much more of each product's nutrition profile so the user can track a healthy diet precisely: net **package size** (enabling per-package macro math), the **fat breakdown** (saturated / mono- / polyunsaturated / trans), **sugars** and **fiber**, **salt / sodium / potassium**, and a compact **micronutrient** set (calcium, iron, vitamins A / C / D). Every new field is **optional/nullable** end-to-end — OFF coverage is sparse and manual entry stays the fallback (FR-006). This is the first S-02 change that alters the schema, so it adds **one EF migration**. The core `MacroNutrients` (kcal/protein/fat/carbs) is left untouched so daily-goal and meal-plan math is unaffected; the extended set lives in a **new owned value object**.

### Changes Required:

#### 1. Domain — `NutritionFacts` value object + Product package size

**File**: `src/Jadlify.Domain/Nutrition/NutritionFacts.cs`, `src/Jadlify.Domain/Products/Product.cs`

**Intent**: Add a per-100g extended-nutrient value object alongside `MacroNutrients`, and a package-size scalar on `Product`.

**Contract**:
- `NutritionFacts` — a record of **all-nullable** `decimal?` per-100g fields: `SaturatedFat, MonounsaturatedFat, PolyunsaturatedFat, TransFat, Sugars, Fiber, Salt, Sodium, Potassium, Calcium, Iron, VitaminA, VitaminC, VitaminD`. Each non-null value must be ≥ 0 (guard in the constructor). Expose `NutritionFacts.Empty` (all null). Pure value object, mirroring `MacroNutrients` (no behavior beyond validation).
- `Product` — add `decimal? PackageSizeGrams` and an owned `NutritionFacts Details`. Extend the constructor with optional params (`decimal? packageSizeGrams = null, NutritionFacts? details = null`) so existing callers keep compiling; default `details` to `NutritionFacts.Empty`. `PackageSizeGrams`, when present, must be > 0.

#### 2. Infrastructure — EF mapping + migration

**File**: `src/Jadlify.Infrastructure/Persistence/Configurations/ProductConfiguration.cs`, new migration under `Persistence/Migrations`

**Intent**: Map the new scalar + owned value object to nullable columns and generate the Postgres migration.

**Contract**:
- Map `Product.PackageSizeGrams` → `package_size_grams` (nullable, `HasPrecision(10, 2)`).
- `builder.OwnsOne(p => p.Details, …)` mapping each field to a nullable column (`saturated_fat_per_100g`, `monounsaturated_fat_per_100g`, `polyunsaturated_fat_per_100g`, `trans_fat_per_100g`, `sugars_per_100g`, `fiber_per_100g`, `salt_per_100g`, `sodium_per_100g`, `potassium_per_100g`, `calcium_per_100g`, `iron_per_100g`, `vitamin_a_per_100g`, `vitamin_c_per_100g`, `vitamin_d_per_100g`). Use **`HasPrecision(12, 6)`** for these — OFF normalizes vitamins/minerals to grams, so values are sub-milligram (e.g. `0.0006` g) and a `(10,2)` precision would truncate them to zero.
- Generate the migration: `dotnet ef migrations add ExtendedNutritionFacts -p src/Jadlify.Infrastructure -s src/Jadlify.API`. SQLite test DBs use `EnsureCreated` and pick the new columns up from the model automatically; the migration targets the real Postgres.
- Confirm `ProductRepository.UpdateAsync` propagates the new scalar + owned values onto the tracked entity (owned types update with the aggregate — verify no manual copy is missed).

#### 3. Infrastructure — OFF adapter mapping (extended nutriments + package size)

**File**: `src/Jadlify.Infrastructure/OpenFoodFacts/OpenFoodFactsResponse.cs`, `OpenFoodFactsBarcodeLookup.cs`

**Intent**: Request and map the additional nutriment keys and the numeric package quantity.

**Contract**:
- Add `product_quantity` (numeric grams) to `OpenFoodFactsProduct`, and the extended `*_100g` keys to `OpenFoodFactsNutriments`: `saturated-fat_100g`, `monounsaturated-fat_100g`, `polyunsaturated-fat_100g`, `trans-fat_100g`, `sugars_100g`, `fiber_100g`, `salt_100g`, `sodium_100g`, `potassium_100g`, `calcium_100g`, `iron_100g`, `vitamin-a_100g`, `vitamin-c_100g`, `vitamin-d_100g`. Add `product_quantity` to the `fields=` allowlist (keep `nutriments`).
- `Map` builds a `NutritionFacts` from the new keys (each `null` when absent) and resolves package size: prefer `product_quantity` (already grams); else parse the leading number from `quantity` text **only when its unit is g/kg** (kg → ×1000); otherwise `null`. Volumes (`ml`/`cl`/`l`) are out of scope for the grams model → `null`.
- All new fields stay non-blocking: any parse failure / missing key yields `null`, never an error (FR-006). Existing all-outcome resilience is preserved.

#### 4. Application — DTOs, commands, validators, lookup mapping

**File**: `src/Jadlify.Application/Products/*` (`ProductDto`, `BarcodeLookupResult`, `IBarcodeProductLookup` `BarcodeProductData`, `CreateProduct/*`, `UpdateProduct/*`, `ListProducts`/`GetProduct` mapping, `LookupBarcode/*`)

**Intent**: Thread the new optional fields through the use-cases.

**Contract**:
- `BarcodeProductData`, `ProductDto`, and `BarcodeLookupResult` gain `decimal? PackageSizeGrams` + the 14 nullable `NutritionFacts` fields.
- `CreateProductCommand`/`UpdateProductCommand` gain the same optional fields; handlers build `NutritionFacts` + set `PackageSizeGrams` on the `Product`.
- Entity→`ProductDto` mapping copies the new fields.
- `LookupBarcodeQuery`: `Found` maps the extended fields from `BarcodeProductData`; `AlreadyInCatalog` maps them from the existing product's `Details` + `PackageSizeGrams`.
- Validators: each present extended field ≥ 0 and ≤ a sane per-100g bound (fats/sugars/fiber/salt ≤ 100 g; sodium/potassium/minerals/vitamins stored in grams ≤ a generous bound, e.g. ≤ 100); `PackageSizeGrams` > 0 and ≤ a large bound (e.g. ≤ 100000 g). Reuse the bound-constant pattern next to the validators. No cross-field coherence checks (e.g. saturated ≤ total fat) — deferred.

#### 5. API — contracts + endpoints

**File**: `src/Jadlify.API/Products/ProductContracts.cs`, `ProductEndpoints.cs`

**Intent**: Expose the new fields over the wire.

**Contract**: Extend `CreateProductRequest`, `UpdateProductRequest`, `ProductResponse`, and `BarcodeLookupResponse` with `PackageSizeGrams` + the 14 nullable extended fields. Endpoints pass them through the commands/queries (including the 201 create body). Update the mapping helpers (`ProductResponse.FromDto`, `BarcodeLookupResponse.FromResult`).

#### 6. Frontend — extended form, pre-fill, and display

**File**: `src/Jadlify.Web/src/products/types.ts`, `ProductFormModal.tsx`, `ProductsPage.tsx` (+ tests)

**Intent**: Let the user see and edit the richer data, pre-filled by lookup, and surface package size + per-package macros.

**Contract**:
- `types.ts`: extend `Product`, `CreateProductRequest`, `UpdateProductRequest`, `BarcodeLookupResponse` with `packageSizeGrams` + the 14 nullable fields.
- `ProductFormModal`: add a **collapsible "Additional nutrition" section** (so the core form stays compact) with inputs for package size + the extended fields, grouped (Fats / Carbohydrates / Minerals / Vitamins). A barcode `Found` pre-fills present extended fields (missing left blank); existing NotFound/AlreadyInCatalog behavior is unchanged. Submit sends the full payload; blank inputs map to `null`.
- `ProductsPage`: show package size on the card and, when present, a computed **per-package** kcal/macros line (`per100g × packageSizeGrams / 100`). Optionally surface a few key extended fields compactly (e.g. saturated fat, sugars, fiber, salt); full detail lives in the edit form.
- Units: store/transmit the raw per-100g grams OFF provides; the display layer formats sub-gram micros as mg/µg.

#### 7. Tests (all layers)

**File**: domain / application / infrastructure / API / web test projects

**Contract**:
- Domain: `NutritionFacts` rejects negatives, accepts nulls; `Product` rejects non-positive `PackageSizeGrams`.
- Application: create/update round-trip the new fields; validators reject out-of-range extended values; `LookupBarcodeQuery` Found/AlreadyInCatalog carry extended fields.
- Infrastructure: OFF adapter maps extended nutriments + `product_quantity`; `quantity`-text fallback (g/kg parsed, volume → null); missing keys → null; the existing all-outcome coverage still passes.
- API: create→get round-trips package size + extended fields; barcode lookup returns them; cross-user isolation still holds.
- Web: form pre-fills extended fields from a Found lookup; per-package macro line renders when package size is set; manual edit of extended fields submits them.

### Success Criteria:

#### Automated Verification:

- [ ] Solution build passes: `dotnet build` (Domain, Application, Infrastructure, API).
- [ ] EF migration `ExtendedNutritionFacts` is generated and applies cleanly (review the generated `Up`/`Down`).
- [ ] Backend tests pass: `pwsh ./.scripts/test-min.ps1` for `tests/Jadlify.Application.Tests`, `tests/Jadlify.Infrastructure.Tests`, and `tests/Jadlify.API.Tests`.
- [ ] Format checks pass for the touched backend projects (`format-min.ps1`).
- [ ] Frontend passes: `npm run lint`, `npm test`, `npm run build` (in `src/Jadlify.Web`).

#### Manual Verification:

- [ ] OFF smoke (`3017624010701`): saturated fat, sugars, salt (and any present micros) + package size map correctly.
- [ ] Add via barcode pre-fills the extended fields OFF has; missing ones stay blank and editable.
- [ ] A product with a package size shows a per-package kcal/macros line in the list.
- [ ] Manual entry/edit of extended fields persists and re-displays after reload.

**Implementation Note**: This is the final phase — confirm the full manual end-to-end walkthrough (including the extended fields and per-package math) before closing the slice.

---

## Testing Strategy

### Unit Tests:

- **Application**: command/query handlers against fake `IProductRepository` + fake `IBarcodeProductLookup`; `LookupBarcodeQuery` ordering (catalog before OFF); validators (boundary values for name length, macro upper bounds, barcode shape).
- **Infrastructure**: `OpenFoodFactsBarcodeLookup` over a stubbed `HttpMessageHandler` for every OFF outcome (found/partial/only-kJ/`status:0`/404/302/timeout/malformed); the rewritten delete test.

### Integration Tests:

- **API**: full pipeline via `WebApplicationFactory<Program>` with SQLite in-memory DB + stubbed lookup + `TestAuthenticationHandler`; CRUD happy paths, validation 400s, delete 204, barcode 200 variants, and **cross-user 404 isolation**.

### Manual Testing Steps:

1. Sign in; open `/products`; add a product manually; confirm it appears.
2. Add via barcode `3017624010701` (Found, pre-filled); save.
3. Enter a code with partial data (only some macros pre-filled); complete and save.
4. Enter an unknown code (empty form, barcode kept); fill manually and save.
5. Re-enter a code already saved (offered the existing product to edit).
6. Edit a product; delete a product (confirm dialog); verify the list updates.
7. Repeat key steps at mobile width.

## Performance Considerations

Single-user MVP, small data volumes — well under the NFR ceiling (~1000 products, <800ms p95). OFF reads are 15 req/min/IP server-side and irrelevant at this scale; no caching needed (data is snapshotted into the row anyway). The OFF call uses a short (~4s) timeout so a slow/missing OFF never stalls product creation. The list query is a single owner-scoped read (indexed on `user_id`).

## Migration Notes

Phases 1–4 require no schema or EF migration: the `products` table, owned macros, barcode column, and `(user_id, barcode)` index are already defined by `ProductConfiguration` (F-02). Removing the in-use delete guard is a behavior change in `ProductRepository`, not a schema change.

**Phase 5 is the exception** — it adds the `package_size_grams` column plus the owned `NutritionFacts` columns, so it introduces **one EF migration** (`ExtendedNutritionFacts`) against Postgres. SQLite test databases use `EnsureCreated`, so they pick the new columns up from the model without running the migration.

## References

- Barcode API decision + alternatives: `context/changes/product-catalog-with-barcode-fallback/research-barcode-api.md`
- OFF usage reference (endpoint, envelope, nutriments §7, field mapping §8, adapter checklist §12): `context/changes/product-catalog-with-barcode-fallback/off-api-reference.md`
- Existing repository (owner scoping, delete guard to remove): `src/Jadlify.Infrastructure/Persistence/Repositories/ProductRepository.cs`
- CQRS mediator contracts: `src/Jadlify.Application/Common/Mediator/IMediator.cs`
- Result/Error model: `src/Jadlify.SharedKernel/Result.cs`, `Error.cs`, `ValidationError.cs`
- API auth pipeline + endpoint style: `src/Jadlify.API/Program.cs`
- API test harness pattern: `tests/Jadlify.API.Tests/Authentication/AuthBoundaryTests.cs`
- Repo test harness: `tests/Jadlify.Infrastructure.Tests/Persistence/SqliteTestDatabase.cs`, `TestCurrentUser.cs`
- Frontend API client + data-hook pattern: `src/Jadlify.Web/src/api/client.ts`, `src/api/useMe.ts`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Application — Product Use-Cases & Barcode Port

#### Automated

- [x] 1.1 Build passes (`build-min.ps1 -Project src/Jadlify.Application`) — 78a39de
- [x] 1.2 Application unit tests pass (handlers + validators) — 78a39de
- [x] 1.3 Format check passes (`format-min.ps1 -Project src/Jadlify.Application`) — 78a39de

#### Manual

- [x] 1.4 `LookupBarcodeQuery` resolves own-catalog hit before OFF — 78a39de
- [x] 1.5 Validators reject impossible values, accept realistic data — 78a39de

### Phase 2: Infrastructure — Open Food Facts Adapter & Delete-Policy Alignment

#### Automated

- [x] 2.1 Build passes (`build-min.ps1 -Project src/Jadlify.Infrastructure`) — a4ca39c
- [x] 2.2 Infrastructure tests pass (adapter + updated delete test) — a4ca39c
- [x] 2.3 Adapter tests cover all OFF outcomes (found/partial/only-kJ/status:0/404/302/timeout/malformed) — a4ca39c
- [x] 2.4 Format check passes (`format-min.ps1 -Project src/Jadlify.Infrastructure`) — a4ca39c

#### Manual

- [x] 2.5 Smoke against production OFF (Nutella `3017624010701`) maps correctly — a4ca39c
- [x] 2.6 Unreachable/timeout `BaseUrl` returns "not found" without throwing — a4ca39c

### Phase 3: API — Product Endpoints & Result→HTTP Mapping

#### Automated

- [x] 3.1 Build passes (`build-min.ps1 -Project src/Jadlify.API`) — f77cbfc
- [x] 3.2 API integration tests pass — f77cbfc
- [x] 3.3 Cross-user isolation test (user B → user A product → 404) passes — f77cbfc
- [x] 3.4 Verify script passes (`verify-min.ps1 -BuildProject src/Jadlify.API -TestProject tests/Jadlify.API.Tests`) — f77cbfc

#### Manual

- [x] 3.5 Manual CRUD over real token returns expected 201/200/204/404/400 — f77cbfc
- [x] 3.6 Barcode lookup endpoint returns Found body and HTTP-200 NotFound — f77cbfc

### Phase 4: Frontend — Products Page (List + Modal Form + Barcode Pre-fill)

#### Automated

- [x] 4.1 Lint passes (`npm run lint`) — 0d0a97d
- [x] 4.2 Frontend tests pass (`npm test`) — 0d0a97d
- [x] 4.3 Production build passes (`npm run build`) — 0d0a97d

#### Manual

- [x] 4.4 Manual add shows product in list — 0d0a97d
- [x] 4.5 Barcode flow: Found / partial / not-found / already-in-catalog all behave per US-02 — 0d0a97d
- [x] 4.6 Edit and delete (with confirmation) update the list — 0d0a97d
- [x] 4.7 Usable at mobile width with visible feedback / progress — 0d0a97d

### Phase 5: Extended Nutrition Facts & Package Size

#### Automated

- [x] 5.1 Solution build passes (`dotnet build`) — cb8f5d0
- [x] 5.2 EF migration `ExtendedNutritionFacts` generated and applies — cb8f5d0
- [x] 5.3 Backend tests pass (Application + Infrastructure + API) — cb8f5d0
- [x] 5.4 Format checks pass for touched backend projects — cb8f5d0
- [x] 5.5 Frontend lint + tests + build pass — cb8f5d0

#### Manual

- [x] 5.6 OFF smoke maps extended fields + package size — cb8f5d0
- [x] 5.7 Barcode pre-fills extended fields (missing stay blank) — cb8f5d0
- [x] 5.8 Per-package macro line shows when package size is set — cb8f5d0
- [x] 5.9 Manual extended-field entry persists and re-displays — cb8f5d0
