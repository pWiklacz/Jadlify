# Daily Goals And Meal Plan - Plan Brief

> Full plan: `context/changes/daily-goals-and-meal-plan/plan.md`

## What & Why

Build roadmap slice S-04: a signed-in user can set daily macro goals and add recipes to a selected day by meal type and portions. This creates the planning input that S-05 will summarize against goals and S-06 will turn into a shopping list.

## Starting Point

Auth, product CRUD, recipe CRUD, recipe macro math, and the protected React shell already exist. Planning domain types, EF mappings, and owner-scoped repositories also exist, but there are no planning application handlers, API endpoints, or real `/goals` and `/meal-plan` pages.

## Desired End State

`/goals` lets the user view and replace one current daily target for calories, protein, fat, and carbohydrates. `/meal-plan` lets the user select one date, add recipes as meal entries with meal type and integer portions, edit meal type/portions, and delete entries. The feature intentionally does not show day-level macro totals, goal deltas, or shopping-list output.

## Key Decisions Made

| Decision | Choice | Why |
| --- | --- | --- |
| S-04 scope | Goals and meal-plan entries only | Keeps the roadmap boundary clean; S-05 owns totals and deltas. |
| Date UX | Single selected day | Matches PRD MVP scope and avoids weekly-plan expansion. |
| Route shape | Keep `/goals` and `/meal-plan` separate | Reuses existing protected navigation and keeps screens focused. |
| Portion model | Positive integers | Matches existing `MealPlanEntry.Portions` and avoids a schema change. |
| Duplicate entries | Allowed | PRD allows repeated recipe usage; hidden uniqueness rules would surprise users. |
| Missing goal | Meal plan still works | A meal entry does not technically require a configured goal. |
| Recipe edits | Entries reference current recipe | Matches existing `recipeId` model and treats the plan as editable, not historical. |
| Entry edit scope | Meal type and portions only | Fits FR-014 and avoids changing date/recipe in edit mode. |

## Scope

**In scope:**

- Daily-goal read/upsert application use-cases and API.
- Meal-plan list/add/update/delete application use-cases and API.
- Owner-scoped recipe lookup for meal-plan display.
- `/goals` current-goal form.
- `/meal-plan` single-day date selector, entry list, add/edit/delete flow.
- Domain, application, infrastructure, API, and frontend tests.
- S-04 contract registry update for later S-05/S-06 work.

**Out of scope:**

- Daily macro totals, goal deltas, progress bars, shopping-list generation.
- Weekly plan view, multi-day shopping list, goal history, decimal portions.
- Recipe snapshots on meal-plan entries.
- Editing an entry's date or recipe.

## Architecture / Approach

Follow the existing vertical-slice pattern: Domain owns meal-entry invariants, Application owns commands/queries/validators, Infrastructure keeps owner-scoped persistence, API maps Minimal API route groups through the mediator, and React consumes those contracts with the shared authenticated API client and React Query.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Planning Application Contracts | Goal and meal-plan commands, queries, validators, handler tests | Accidentally leaking S-05 summary behavior into S-04. |
| 2. Planning API And Integration Tests | `/api/daily-goal`, `/api/meal-plan`, owner-scoped API tests | Cross-user recipe or entry access must fail safely. |
| 3. Goals And Meal Plan React UI | Real `/goals` and `/meal-plan` screens | Keeping the UI useful without adding totals/deltas early. |
| 4. Verification And Handoff | Narrow verification plus contract docs | Future S-05/S-06 need stable route and semantics handoff. |

**Prerequisites:** S-03 recipe builder is complete and `/api/recipes` works.
**Estimated effort:** ~4 focused sessions across 4 phases.

## Open Risks & Assumptions

- No schema migration should be needed because the selected behavior matches the existing model.
- Recipe list volume is MVP-small; planning display should still use batch lookup for selected-day recipe ids.
- Future S-05 may add daily summary DTOs; this plan intentionally leaves that contract open.

## Success Criteria (Summary)

- A user can set and reload their current daily macro goal.
- A user can add, list, edit, and delete meal-plan entries for one selected day.
- User-owned goals and meal-plan entries remain isolated, and no S-05/S-06 behavior appears in S-04.
