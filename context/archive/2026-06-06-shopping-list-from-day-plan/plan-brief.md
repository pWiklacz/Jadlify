# Shopping List From Day Plan - Plan Brief

> Full plan: `context/changes/shopping-list-from-day-plan/plan.md`

## What & Why

Build roadmap slice S-06: a signed-in user can generate and view a deduplicated shopping list from the selected day plan. This closes the shopping consequence of Jadlify's MVP flow: the day plan becomes both macro feedback and a practical list of ingredients to buy.

## Starting Point

S-05 already computes daily macro summaries from owner-scoped meal-plan entries and recipe ingredient snapshots. `/shopping-list` exists in protected navigation, but it currently renders only a placeholder.

## Desired End State

`/shopping-list` lets the user select one date and see a read-only, A-Z shopping list for that day's plan. Each product appears once with summed grams formatted to one decimal place. Empty days show a normal empty state and link to `/meal-plan`; defensive missing recipes produce warnings instead of silently hiding the issue.

## Key Decisions Made

| Decision | Choice | Why |
| --- | --- | --- |
| Surface | Dedicated `/shopping-list` page | Uses existing navigation and keeps `/meal-plan` focused on planning and macro feedback. |
| Generation model | Compute on read | Avoids persisted list snapshots and stale refresh semantics. |
| Empty day | Empty list with CTA | A day without entries is a normal state, not an API error. |
| Quantity scaling | `WholeRecipeAmount * entry.Portions / recipe.Portions` | Matches existing portion semantics and macro calculation behavior. |
| Aggregation identity | Ingredient snapshot `ProductId` | Deduplicates deterministically without reloading current product rows. |
| Missing recipe | Omit contribution and return warning | Keeps the list usable while surfacing a defensive data gap. |
| Item detail | Product name + summed grams | Meets FR-015/FR-016 without adding per-recipe breakdown scope. |
| Display format | A-Z, grams to one decimal | Predictable for users and consistent with existing one-decimal formatting. |

## Scope

**In scope:**

- Read-only shopping-list calculation for one selected day.
- Aggregation by snapshot product id and summed grams.
- `GET /api/shopping-list?date=yyyy-MM-dd`.
- React `/shopping-list` page with date selector, loading/error/empty states, warnings, and item list.
- Backend, API, frontend tests, and S-06 contract registry entry.

**Out of scope:**

- Persisted shopping lists, checked-state, export, multi-day lists, product categories, pantry/no-waste, and non-gram units.
- Per-recipe source breakdown on each item.
- Adding shopping-list output to `/meal-plan`.

## Architecture / Approach

The slice follows Domain -> Application -> API -> React. A shopping-list calculator scales recipe ingredient snapshots by planned portions and aggregates by product id. A query handler reads one day's owner-scoped meal-plan entries plus ingredient-rich recipes, returns items and warnings, and the API exposes that as a protected read endpoint consumed by the React page.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Domain/Application projection | Calculator, DTOs, query handler, unit tests | Incorrect portion scaling would make the list untrustworthy. |
| 2. API endpoint | `GET /api/shopping-list?date=`, response contracts, integration tests | User isolation and warning serialization must be explicit. |
| 3. React page | Real `/shopping-list` UI with date selector and states | Page must stay useful without growing into multi-day or checked-list scope. |
| 4. Contracts/verification | S-06 contract registry entry and final gates | Future changes need one stable aggregation contract. |

**Prerequisites:** S-05 is complete; meal-plan entries and recipes with ingredient snapshots exist.
**Estimated effort:** ~3 focused sessions across 4 phases.

## Open Risks & Assumptions

- Amounts may be fractional grams after portion scaling; formatting rounds only for display.
- Missing recipe warnings are defensive; normal recipe deletion is blocked while a meal-plan entry references it.
- The implementation may choose whether the page component lives under `src/shopping` with a thin route wrapper, following existing feature-folder patterns.

## Success Criteria (Summary)

- The same product used across multiple planned recipes appears once with correctly summed grams.
- `/shopping-list` works for planned and empty days, with warnings for defensive data gaps.
- Shopping-list data remains owner-scoped and is computed on read, with no new persistence.
