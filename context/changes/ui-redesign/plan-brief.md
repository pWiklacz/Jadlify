# UI Redesign and Expanded Planning — Plan Brief

> Full plan: `context/changes/ui-redesign/plan.md`
> Research: `context/changes/ui-redesign/research.md`

## What & Why

Jadlify dostanie spójny, polski i responsywny interfejs zgodny z siedmioma mockupami z `docs/design/`. Zmiana obejmuje też świadomie wybrane rozszerzenia: pełny planer, trwałe listy zakupów, metadane produktów, bogatsze katalogi oraz interaktywny dashboard.

## Starting Point

Pełny flow MVP działa end-to-end, a hooki danych są dobrze oddzielone od prezentacji. UI jest jednak surowy; planer obsługuje jeden dzień i całkowite porcje, shopping jest nietrwałą projekcją jednego dnia, a strona główna placeholderem.

## Desired End State

Użytkownik planuje przepisy i pojedyncze produkty w widokach dnia, tygodnia i miesiąca, przenosi/kopiuje wpisy, generuje trwałe listy z wielu dni oraz bezpiecznie synchronizuje je po zmianie planu. Wszystkie ekrany korzystają z jednego design systemu, polskiego copy i tych samych dostępnych komponentów.

## Key Decisions Made

| Decision | Choice | Why | Source |
| --- | --- | --- | --- |
| Granica mockupów | Wybrane rozszerzenia | Planner/shopping/product metadata wchodzą, konto i goal API są odłożone | Plan |
| Dashboard | Interaktywny dzień + „Następny krok” | Domyka pustą stronę bez globalnej checklisty | Plan |
| Planner | Dzień/tydzień/miesiąc, move/copy, recipe 0.5 i product grams | Realizuje planowanie z wyprzedzeniem | Plan |
| Planner source | Recipe reference lub product snapshot | Zachowuje semantykę przepisu i odporność usuwalnego produktu | Research / Plan |
| Zakupy | Active/history + jawny diff | Brak cichej zmiany listy i utraty postępu | Plan |
| Diff bought | Unchanged zachowuje; Added/Changed resetuje | Zgodne z mockupem | Research |
| Produkt | Opcjonalna marka i kategoria | Wspiera filtry i grupowanie, zachowuje „Bez kategorii” | Research / Plan |
| Handoff | URL/query params, bez localStorage | Deep-linkowalne, bez shadow state | Plan |
| Realizacja | Jeden plan, 9 twardych faz | Jedna wizja z osobnymi gates | Plan |

## Scope

**In scope:**

- Tokeny, fonty, shell, prymitywy UI, polskie copy i breakpoint 940 px.
- Produkty/przepisy/cele: metadane, katalogi, detail, builder, Add-to-plan i docelowe stany celu.
- Planner/listy: dzień/tydzień/miesiąc, product entries, batch actions, trwałość, historia i diff.
- Dashboard dnia, migracja RTL/E2E oraz manual visual/accessibility QA.

**Out of scope:**

- Account lifecycle oraz delete/timestamp/historia celów.
- Drag-and-drop, eksport, camera scanning, offline/native/AI/social, globalna checklista i localStorage jako źródło danych.

## Architecture / Approach

Backend pozostaje autorytatywny i owner-scoped. Planner dostaje bounded range read (maks. 42 dni) i atomowe komendy batch; product entries snapshotują produkt, recipe entries zachowują reference + delete conflict. Trwała lista jest osobnym agregatem snapshotowym z optimistic concurrency i potwierdzanym diffem. Frontend korzysta ze wspólnych `src/ui/*`, range query keys i mutation hooks współdzielonych z dashboardem.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. UI foundation | Tokeny, prymitywy, shell i auth | Focus/accessibility drift |
| 2. Product contracts | Brand/category, migracja i catalog API | Data migration/ownership |
| 3. Product UI | Index, filters, detail i forms | Query/cache complexity |
| 4. Recipes & goals | Catalog/detail/Add-to-plan i goal UX | Heavy reads/handoff |
| 5. Planner core | Discriminated source, decimal i range | Macro correctness/migration |
| 6. Planner operations | Atomic move/copy/copy-day | Partial writes/IDOR |
| 7. Planner UI | Day/week/month and dialogs | State/cache/performance |
| 8. Persistent shopping | Aggregate, history, diff and API | Concurrency/data loss |
| 9. UI & integration | Shopping UI, dashboard, E2E/smoke | Cross-feature regression |

**Prerequisites:** branch `feature/ui-redesign`, lokalny Supabase dla API/E2E, kopia bazy przed migracjami i sekwencyjne minimalne skrypty.

**Estimated effort:** bardzo duży change — około 20–30 skupionych sesji implementacyjnych przez 9 faz, z manualnym gate po każdej fazie.

## Open Risks & Assumptions

- Range i katalogi pozostają batchowe/paginowane, aby utrzymać NFR <800 ms.
- Recipe edit nadal wpływa na istniejący plan; tylko bezpośredni product entry jest snapshotem.
- Stale shopping refresh zwraca 409 i wymusza nowy preview; nie auto-merge'uje.
- Fonty i OFF nie mogą blokować renderu ani ręcznego dodania produktu.
- E2E zależy od lokalnego auth/backendu; manualny smoke pozostaje obowiązkowy.

## Success Criteria (Summary)

- Pełny product→recipe→goal→week/month plan→persistent shopping flow działa po polsku na desktopie i mobile.
- Makro, gramatury, batch operations, ownership i shopping diff są chronione testami i transakcjami.
- Wszystkie gates przechodzą, a każdy ekran przechodzi manualne porównanie 1440/940/390 px.
