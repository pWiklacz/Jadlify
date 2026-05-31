---
change_id: product-catalog-with-barcode-fallback
doc: reference
topic: Open Food Facts API — usage reference for barcode lookup
created: 2026-05-31
updated: 2026-05-31
api_version: v2
companion: research-barcode-api.md
---

# Open Food Facts API — usage reference (for S-02 barcode lookup)

> Practical reference for implementing the barcode → product lookup in S-02.
> Scope: the **read / Get-product-by-barcode** path Jadlify needs. Write/edit
> and full search are documented briefly at the end for completeness but are
> **out of scope** for the MVP (we snapshot into our own DB; see
> [`research-barcode-api.md`](./research-barcode-api.md)).

## 1. At a glance

- **Current version:** `v2` (stable). `v3` exists but is in active development and changes frequently — **use v2**.
- **License:** database under Open Database License (ODbL); product images under CC BY-SA. Data is crowdsourced — "no assurances it is accurate, complete, or reliable."
- **Auth for reads:** none required — just a custom `User-Agent`. (Writes require account auth — not used by us.)
- **Format:** JSON over HTTPS, `GET`.
- **Base (production):** `https://world.openfoodfacts.org`
- **Base (staging):** `https://world.openfoodfacts.net` (HTTP Basic auth `off` / `off`, to block search-engine indexing). Use staging for tests.

## 2. Authentication & required headers

Reads need **no token/key**. You must send a descriptive `User-Agent`, or you risk being treated as a bot and IP-banned.

Format: `AppName/Version (ContactEmail)`

```
User-Agent: Jadlify/0.1 (contact@jadlify.example)
```

> Reads = only the User-Agent. Writes (POST/PUT/DELETE) additionally need a
> Open Food Facts account (`user_id` + `password`, or a session cookie) plus
> `app_name` / `app_version` / `app_uuid`. We do **not** write, so ignore this.

## 3. Rate limits

| Operation | Limit |
|---|---|
| Product reads (`GET /api/v*/product`) | **15 req / min / IP** |
| Search (`GET /api/v*/search`) | **10 req / min / IP** (do not use for search-as-you-type) |
| Global abuse guard | HTTP `503` returned regardless of IP if overall limits exceeded |

- For mobile apps the limit is **per end-user IP**; for a server-side backend (our case, per F-01) it is **per server IP** — so funnel calls through the backend and cache results into our own product row.
- If you ever need to fetch hundreds+ of products, download the CSV / JSONL exports instead of hammering the API.

## 4. Get a product by barcode (the endpoint we use)

```
GET https://world.openfoodfacts.org/api/v2/product/{barcode}
GET https://world.openfoodfacts.org/api/v2/product/{barcode}.json   # .json is optional, same result
```

### Path parameter

| Param | Description |
|---|---|
| `barcode` | The product barcode (EAN-8, EAN-13, UPC-A, UPC-E). Send the digits **as scanned/typed** — OFF normalizes them server-side (pads to 13 digits, etc.). Do **not** normalize client-side. |

### Useful query parameters

| Param | Purpose | Example |
|---|---|---|
| `fields` | Comma-separated allowlist of fields to return. **Always set this** to shrink the payload (a full product object is huge). | `fields=product_name,brands,nutriments,quantity` |
| `lc` | Language code(s) for localized fields, comma-separated. | `lc=pl,en` |
| `cc` | Country context (affects some computed values). | `cc=pl` |
| `knowledge_panels` | Return ready-to-render "knowledge panels" (not needed for us). | — |
| `blame=1` | Add per-field "who last edited" metadata (not needed). | — |

**Recommended request for Jadlify:**

```
GET https://world.openfoodfacts.org/api/v2/product/{barcode}?fields=product_name,brands,quantity,nutriments&lc=pl,en
```

## 5. Response envelope

Every product response is wrapped in this envelope:

```json
{
  "code": "3017624010701",
  "status": 1,
  "status_verbose": "product found",
  "product": { /* the fields you requested */ }
}
```

| Field | Type | Meaning |
|---|---|---|
| `code` | string | The (normalized) barcode that was queried. |
| `status` | int | **`1` = found**, **`0` = not found**. |
| `status_verbose` | string | Human-readable status (`"product found"` / `"product not found"`). |
| `product` | object | Present only when `status = 1`. Contains exactly the fields named in `fields`. |

> **Found / not-found handling (important for FR-006 fallback):**
> The `.json` product route typically returns **HTTP 200** even when the product
> is absent, signalling absence via `"status": 0`. (The OpenAPI spec also
> documents a `404` for not-found, and a `302` redirect when a barcode belongs
> to a different OFF project e.g. Open Beauty Facts.) **Branch on the `status`
> field, not solely on the HTTP code.** `status: 0` is *not* an error — route
> straight into manual entry.

### Not-found example

```json
{
  "code": "0000000000000",
  "status": 0,
  "status_verbose": "product not found"
}
```

## 6. The `product` object — fields relevant to us

Request only what you store. The most relevant fields:

| Field | Type | Notes |
|---|---|---|
| `product_name` | string | Generic/default name. |
| `product_name_{lc}` | string | Localized name, e.g. `product_name_pl`. Prefer when present. |
| `brands` | string | Comma-separated brand string (e.g. `"Ferrero, Nutella"`). |
| `brands_tags` | string[] | Normalized brand slugs. |
| `quantity` | string | Net package quantity as text (e.g. `"400 g"`). |
| `serving_size` | string | Serving size as text (e.g. `"15 g"`). |
| `nutriments` | object | Nutrition values — see §7. **This is the core for kcal/macros.** |
| `categories_tags` | string[] | Taxonomy categories (e.g. `en:chocolate-spreads`). |
| `labels_tags` | string[] | Labels (organic, vegan…). |
| `nutriscore_grade` / `nutrition_grades` | string | `a`–`e` Nutri-Score (optional, not needed for MVP). |
| `nova_group` | int | 1–4 ultra-processing class (optional). |
| `image_front_url` / `image_url` | string | Product image URLs (optional). |
| `ingredients_text` / `ingredients_text_{lc}` | string | Ingredient list (optional). |
| `allergens_tags` | string[] | Allergens (optional). |
| `lang` | string | Main language of the product entry. |

## 7. The `nutriments` object — naming convention

This is the part to understand well. Each nutrient appears under **multiple
suffixed keys**. For a nutrient `X` (e.g. `energy-kcal`, `proteins`, `fat`):

| Key | Meaning |
|---|---|
| `X` | Value in the product's own base (`nutrition_data_per`, which may be `100g` or `serving`). **Ambiguous — avoid.** |
| `X_value` | Numeric value exactly as entered. |
| `X_unit` | Unit of the entered value (e.g. `g`, `kcal`, `kJ`). |
| **`X_100g`** | **Value normalized per 100 g — use this for deterministic per-100g storage.** |
| `X_serving` | Value per serving. |
| `X_prepared`, `X_prepared_100g`, … | Same set for the *prepared* product (e.g. powder reconstituted). |

Note: nutriment keys use **hyphens**, e.g. `energy-kcal`, `saturated-fat`.

### Common nutrient keys

`energy`, `energy-kj`, `energy-kcal`, `fat`, `saturated-fat`, `carbohydrates`,
`sugars`, `fiber`, `proteins`, `salt`, `sodium`.

### Example (`fields=...,nutriments`, Nutella `3017624010701`)

```json
"nutriments": {
  "energy": 2255,
  "energy-kcal": 539,
  "energy-kcal_100g": 539,
  "energy-kcal_unit": "kcal",
  "carbohydrates": 57.5,
  "carbohydrates_100g": 57.5,
  "carbohydrates_unit": "g",
  "carbohydrates_value": 57.5,
  "sugars": 56.3,
  "sugars_100g": 56.3,
  "sugars_unit": "g",
  "proteins_100g": 6.3,
  "fat_100g": 30.9,
  "salt_100g": 0.107
}
```

## 8. Field mapping → Jadlify product row

Snapshot at add-time (grams / per-100g model, per F-02):

| Jadlify field | OFF source | Fallback if missing |
|---|---|---|
| name | `product_name_pl` → `product_name` | manual entry |
| brand (optional) | `brands` | empty |
| package quantity (optional) | `quantity` | empty |
| kcal / 100g | `nutriments.energy-kcal_100g` | derive from `energy-kj_100g ÷ 4.184`; else manual |
| protein / 100g | `nutriments.proteins_100g` | manual |
| carbs / 100g | `nutriments.carbohydrates_100g` | manual |
| fat / 100g | `nutriments.fat_100g` | manual |

> **Always handle missing nutriment keys gracefully.** Crowdsourced products
> frequently lack one or more nutriments (and sometimes give only `energy-kj`,
> not `energy-kcal`). Treat any missing macro as "prefill what exists, let the
> user complete the rest" — which is the FR-006 fallback path anyway.

## 9. Bulk lookup (optional)

The search endpoint accepts comma-separated codes — handy if you ever batch:

```
GET https://world.openfoodfacts.org/api/v2/search?code=3017624010701,8437011606013&fields=code,product_name,nutriments
```

## 10. Search endpoint (reference only — not used in S-02 MVP)

```
GET https://world.openfoodfacts.org/api/v2/search?<tag filters>&fields=...&sort_by=...
```

- Filter by taxonomy tags, e.g. `categories_tags_en=Orange Juice&nutrition_grades_tags=c`.
- `fields` limits returned fields per product; `sort_by` (e.g. `last_modified_t`) orders results.
- Paginated response envelope: `count`, `page`, `page_size`, `page_count`, `products[]`, `skip`.
- **Full-text search is only in the v1 search API** (or the beta `search-a-licious` project) — v2 search is filter-based. Not needed for barcode MVP.

## 11. Write API (out of scope — documented for awareness)

Contributing data back (e.g. when a scanned product is missing nutriments) is
possible via `POST https://world.openfoodfacts.org/cgi/product_jqm2.pl` with
`user_id` + `password` + `code` + `nutriment_*` fields. **MVP does not write
back** — we keep the integration read-only. Revisit only if we later want to
enrich OFF from our manual-entry data (post-MVP, would need account + consent).

## 12. Backend adapter checklist (for /10x-plan)

- [ ] Lookup lives **server-side** behind our own API endpoint (F-01); browser never calls OFF directly.
- [ ] Send `User-Agent: Jadlify/<ver> (<contact>)` on every request.
- [ ] Call `GET /api/v2/product/{barcode}?fields=product_name,product_name_pl,brands,quantity,nutriments&lc=pl,en`.
- [ ] Do **not** normalize the barcode client-side; pass digits through.
- [ ] Branch on body `status` (`1` found / `0` not found), tolerate HTTP `404`/`302` as "not found / treat as miss".
- [ ] Map fields per §8; on any missing macro, prefill what exists and fall through to manual entry (FR-006).
- [ ] Snapshot mapped values into the user's product row (frozen at add-time, user-editable afterwards).
- [ ] Keep the source behind a thin adapter interface so it can be swapped (e.g. FatSecret Premier) without touching the rest of the slice.
- [ ] Set a short HTTP timeout + treat OFF outage as "fall back to manual entry" — barcode must never block product creation.
- [ ] (Optional) Use the **staging** base `https://world.openfoodfacts.net` (Basic `off:off`) in tests so we never touch production data.

## 13. Sources

- API introduction (auth, rate limits, environments): https://openfoodfacts.github.io/openfoodfacts-server/api/
- Tutorial (get product by barcode, Nutri-Score, search): https://openfoodfacts.github.io/openfoodfacts-server/api/tutorial-off-api/
- Cheatsheet (fields param, bulk, suggestions): https://openfoodfacts.github.io/openfoodfacts-server/api/ref-cheatsheet/
- Barcode-scanning tutorial (normalization, status handling): https://openfoodfacts.github.io/openfoodfacts-server/api/tutorials/scanning-barcodes/
- OpenAPI v2 reference: https://openfoodfacts.github.io/openfoodfacts-server/api/ref-v2/
- OpenAPI source (api.yaml): https://github.com/openfoodfacts/openfoodfacts-server/blob/main/docs/api/ref/api.yaml
