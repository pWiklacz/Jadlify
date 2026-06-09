---
change_id: product-catalog-with-barcode-fallback
doc: research
topic: barcode food-product lookup API selection
created: 2026-05-31
updated: 2026-05-31
decision: Open Food Facts API
resolves: roadmap Open Question #3 (Strategia danych po kodzie kreskowym)
---

# Research: Barcode → food product info API for S-02

> Question driving this research: which API should S-02 use to fetch product
> info (name, brand, kcal/macros) from a manually entered barcode, with manual
> entry as the fallback path (FR-006)?

## Decision

**Use the Open Food Facts (OFF) API.** Snapshot the few needed fields into the
user's own product row at add-time; let the user edit/complete via the existing
manual-entry fallback. This resolves roadmap Open Question #3.

## Why OFF wins for this slice

Scored against S-02 / project constraints (low-complexity MVP, single-user,
EU/Poland market, grams as the common unit, barcode is a helper not a blocking
integration, zero budget):

| S-02 / project constraint | How OFF satisfies it |
|---|---|
| Barcode lookup must be **non-blocking** (FR-006 fallback) | `GET` by barcode returns `"status": 0` when not found → branch straight into manual entry. No error gymnastics. |
| **Grams / per-100g** model (F-02; roadmap Q2) | Returns `nutriments.energy-kcal_100g`, `proteins_100g`, `carbohydrates_100g`, `fat_100g` — already per-100g, matches the unit decision. |
| **EU / Polish products** | French-origin, EU-strongest open DB (~4M food products). US-centric options (Nutritionix, USDA) miss most Polish supermarket items. |
| **Freeze data at add-time** (roadmap Q3) | Copy needed fields into the user-owned product row once; crowdsourced-quality risk evaporates because you snapshot, then user can edit. |
| **Cost / no key** | Free, no API key for reads, ODbL-licensed. No `$499–$999/mo` (Nutritionix) or "contact us" (FatSecret) gate. |

## Integration notes (for /10x-plan)

Per F-01, the lookup goes through the **backend API**, not the browser.

Endpoint (request only the fields you store):

```
GET https://world.openfoodfacts.org/api/v2/product/{barcode}?fields=product_name,brands,nutriments
```

Example product: Nutella = `3017624010701`.

Implementation caveats:

1. **`User-Agent` is required etiquette** — format
   `Jadlify - Web - Version <x.y> - <url>`. Default/anonymous UAs get throttled.
2. **Rate limit: 15 req/min/IP** for product reads (10 req/min for `/search`).
   Irrelevant for a single-user MVP, but reinforces that the call belongs
   server-side (which F-01 mandates anyway), not in a per-keystroke client loop.
3. **Barcode normalization** — OFF normalizes EAN-8/EAN-13/UPC-A on its side
   (pads to 13 digits). Do **not** normalize client-side; send the scanned/typed
   digits as-is.
4. **Found vs not-found** — `"status": 1` → parse `product` (`product_name`,
   `brands`, `nutriments.*_100g`). `"status": 0` → show manual-entry form.

Fields to map into the user's product row:

| OFF field | Jadlify product field |
|---|---|
| `product_name` | name |
| `brands` | brand (optional) |
| `nutriments.energy-kcal_100g` | kcal / 100g |
| `nutriments.proteins_100g` | protein / 100g |
| `nutriments.carbohydrates_100g` | carbs / 100g |
| `nutriments.fat_100g` | fat / 100g |

## Alternatives considered (and why not, for MVP)

| API | Coverage / strengths | Why not now |
|---|---|---|
| **FatSecret Platform** | 90%+ barcode hit rate, 58 countries, verified/curated, 26 languages | Free Basic tier is **US-only**; Polish/EU localized data is paid "contact us". Best *upgrade path* if OFF data quality ever becomes a real complaint. |
| **Edamam Food DB** | ~615K UPCs, 28 nutrients, NLP | $69/mo entry; US-leaning coverage. |
| **Nutritionix** | Industry standard US barcode DB (1M+ branded) | **$499+/mo**, US-biased, annual billing. Wrong shape for an MVP. |
| **USDA FoodData Central** | Free + key, authoritative | US branded foods only; no Polish products. |
| **ChowAPI / appnutra / commercial barcode scrapers** | Dev-friendly, credit packs | Smaller/less-proven DBs; paid; no EU advantage over OFF. |

## Upgrade trigger

If users report frequent "product not found" or wrong data for Polish items,
re-evaluate **FatSecret Premier** (localized PL dataset). The OFF integration is
deliberately a thin adapter (barcode → mapped fields), so swapping the source
later is a localized change behind the backend lookup endpoint.

## Sources

- Open Food Facts API docs: https://openfoodfacts.github.io/openfoodfacts-server/api/
- OFF barcode tutorial: https://openfoodfacts.github.io/documentation/docs/Product-Opener/api/tutorial-off-api/
- OFF OpenAPI ref: https://github.com/openfoodfacts/openfoodfacts-server/blob/main/docs/api/ref/api.yaml
- FatSecret editions/pricing: https://platform.fatsecret.com/api-editions
- Nutritionix API pricing: https://www.nutritionix.com/api
- Edamam Food DB: https://developer.edamam.com/food-database-api
- USDA FoodData Central: https://fdc.nal.usda.gov/api-guide
