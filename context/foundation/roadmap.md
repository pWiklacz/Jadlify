---
project: Jadlify
version: 1
status: draft
created: 2026-05-26
updated: 2026-05-27
prd_version: 1
main_goal: low-complexity
top_blocker: capacity
---

# Roadmap: Jadlify

> Derived from `context/foundation/prd.md` (v1) + auto-researched codebase baseline.
> Edit-in-place; archive when superseded.
> Slices below are listed in dependency order. The "At a glance" table is the index.

## Vision recap

Jadlify ma usunac tarcie z planowania posilkow z wyprzedzeniem: reczne liczenie kcal i makro, przepisywanie skladnikow oraz skladanie listy zakupow z wielu przepisow. Produkt traktuje plan posilkow i liste zakupow jako pierwszorzedne wyniki, a sledzenie kalorii jako konsekwencje planu. MVP celowo zostaje przy pojedynczym dniu planu i gramach jako wspolnej jednostce, zeby jak najszybciej domknac jeden uzyteczny przeplyw.

## North star

**S-06: Uzytkownik moze wygenerowac liste zakupow z planu dnia** - W tym dokumencie north star oznacza najmniejszy przeplyw end-to-end, ktory pokazuje, ze produkt dziala jako plan posilkow, a nie tylko jako lista encji; S-06 domyka plan dnia, podsumowanie makro i zakupowa konsekwencje tego planu.

## At a glance

| ID | Change ID | Outcome (user can ...) | Prerequisites | PRD refs | Status |
|---|---|---|---|---|---|
| F-01 | account-data-boundary | (foundation) minimalny mechanizm konta i granica danych uzytkownika sa gotowe do pierwszych zasobow | - | Access Control, NFR Izolacja danych, NFR Prywatnosc operacyjna | ready |
| F-02 | persistent-user-resources | (foundation) zasoby uzytkownika i deterministyczne obliczenia maja trwala, testowalna sciezke | F-01 | NFR Determinizm kalkulacji, NFR Czas odpowiedzi | proposed |
| F-03 | responsive-app-shell | (foundation) responsywny shell aplikacji obsluguje logowanie i pierwszy pionowy przeplyw | F-01 | NFR Responsywnosc dla uzytkownika, NFR Wsparcie urzadzen i przegladarek | proposed |
| S-01 | account-sign-in-flow | user can create an account, sign in, sign out, and reach only the protected app surface | F-01, F-03 | US-01, FR-001, FR-002 | proposed |
| S-02 | product-catalog-with-barcode-fallback | user can add, review, edit, and delete their own products, with barcode lookup falling back to manual entry | S-01, F-02 | US-01, US-02, FR-003, FR-004, FR-005, FR-006 | proposed |
| S-03 | recipe-builder-with-macro-calculation | user can build recipes from their products and see deterministic recipe macro totals | S-02 | US-01, FR-007, FR-008, FR-009, FR-010 | proposed |
| S-04 | daily-goals-and-meal-plan | user can set daily macro goals and add recipes to a selected day by meal type and portions | S-03 | US-01, FR-011, FR-012, FR-014 | proposed |
| S-05 | daily-macro-summary | user can see kcal and macro totals for a selected day plus the numeric delta against their goals | S-04 | US-01, FR-013 | proposed |
| S-06 | shopping-list-from-day-plan | user can generate and view a deduplicated shopping list from the selected day plan | S-05 | US-01, US-03, FR-015, FR-016 | proposed |

## Baseline

What's already in place in the codebase as of `2026-05-26` (auto-researched + user-confirmed).
Foundations below assume these are present and do NOT re-scaffold them.

- **Frontend:** absent - no `package.json` or `ClientApp`; only backend entrypoints are present.
- **Backend / API:** partial - ASP.NET Core Minimal API exists with `/health` and scaffolded `/weatherforecast` in `src/Jadlify.API/Program.cs`.
- **Data:** provider selected, implementation absent - Supabase Postgres is the MVP database target, but there is no DB driver, ORM, migrations, schema, or seed data; `Infrastructure` is only a project shell.
- **Auth:** provider selected, implementation absent - Supabase Auth is the MVP identity provider, but there is no token verification, auth middleware, route authorization, or protected app surface yet.
- **Deploy / infra:** partial - Azure App Service F1 target, deploy runbook, and GitHub Actions workflow exist; no infrastructure-as-code.
- **Observability:** partial - simple `/health` endpoint and default logging config exist; no metrics, error tracking, dashboards, or dedicated request logging.

## Foundations

### F-01: Account Data Boundary

- **Outcome:** (foundation) Minimalny mechanizm konta i granica danych uzytkownika sa gotowe do pierwszych zasobow.
- **Change ID:** account-data-boundary
- **PRD refs:** Access Control, NFR Izolacja danych, NFR Prywatnosc operacyjna
- **Unlocks:** S-01, S-02, S-03, S-04, S-05, S-06
- **Prerequisites:** -
- **Parallel with:** -
- **Blockers:** -
- **Unknowns:** -
- **Planning guidance:**
  - Use Supabase Auth as the identity provider; the frontend may use Supabase only for sign-up/sign-in/session management.
  - ASP.NET Core API must validate Supabase JWT bearer tokens and treat the token `sub` claim as the stable application user id.
  - Define a backend current-user abstraction and authorization boundary that every later user-owned resource can consume.
  - Do not expose core domain tables directly to the browser for MVP behavior; product, recipe, goal, meal-plan, macro-summary, and shopping-list operations must go through the API.
  - Add tests or contracts proving user-scoped operations cannot cross user boundaries.
- **Risk:** Bez tej granicy kazdy pozniejszy zasob grozi naruszeniem najtwardszego guardraila PRD.
- **Status:** ready

### F-02: Persistent User Resources

- **Outcome:** (foundation) Zasoby uzytkownika i deterministyczne obliczenia maja trwala, testowalna sciezke.
- **Change ID:** persistent-user-resources
- **PRD refs:** NFR Determinizm kalkulacji, NFR Czas odpowiedzi
- **Unlocks:** S-02, S-03, S-04, S-05, S-06
- **Prerequisites:** F-01
- **Parallel with:** F-03
- **Blockers:** -
- **Planning guidance:**
  - Use Supabase Postgres as the persistent database and EF Core with Npgsql for backend data access.
  - Store Supabase database connection strings and auth settings outside committed `appsettings*.json` files; use local user secrets and Azure App Service application settings.
  - All persisted user-owned tables must carry the authenticated user id and be queried through backend-scoped repositories/handlers.
  - Keep Supabase Row Level Security as optional defense in depth; do not depend on direct browser-to-table access for MVP domain data.
- **Unknowns:**
  - Czy MVP zostaje przy gramach jako jedynej jednostce produktu? Owner: user. Block: no.
  - Czy gramatura skladnika w przepisie oznacza ilosc dla calego przepisu czy per porcja? Owner: implementator. Block: no.
- **Risk:** Data layer jest najlatwiej przeskalowac zbyt szeroko; ten fundament ma obsluzyc tylko zasoby wymagane przez pierwszy dzienny przeplyw.
- **Status:** proposed

### F-03: Responsive App Shell

- **Outcome:** (foundation) Responsywny shell aplikacji obsluguje logowanie i pierwszy pionowy przeplyw.
- **Change ID:** responsive-app-shell
- **PRD refs:** NFR Responsywnosc dla uzytkownika, NFR Wsparcie urzadzen i przegladarek
- **Unlocks:** S-01, S-02, S-03, S-04, S-05, S-06
- **Prerequisites:** F-01
- **Parallel with:** F-02
- **Blockers:** -
- **Unknowns:** -
- **Risk:** Frontend nie istnieje, ale przy celu low-complexity shell powinien wspierac przeplyw, a nie stac sie osobnym projektem produktowym.
- **Status:** proposed

## Slices

### S-01: Account Sign-In Flow

- **Outcome:** user can create an account, sign in, sign out, and reach only the protected app surface.
- **Change ID:** account-sign-in-flow
- **PRD refs:** US-01, FR-001, FR-002
- **Prerequisites:** F-01, F-03
- **Parallel with:** -
- **Blockers:** -
- **Unknowns:** -
- **Risk:** To pierwszy user-visible test izolacji danych; jesli konto bedzie traktowane jako detal techniczny, pozniejsze zasoby beda trudne do zabezpieczenia konsekwentnie.
- **Status:** proposed

### S-02: Product Catalog With Barcode Fallback

- **Outcome:** user can add, review, edit, and delete their own products, with barcode lookup falling back to manual entry.
- **Change ID:** product-catalog-with-barcode-fallback
- **PRD refs:** US-01, US-02, FR-003, FR-004, FR-005, FR-006
- **Prerequisites:** S-01, F-02
- **Parallel with:** -
- **Blockers:** -
- **Unknowns:**
  - Co dzieje sie z przepisami i planami po usunieciu uzywanego produktu? Owner: user. Block: no.
  - Czy dane produktu pobrane po kodzie kreskowym zostaja zamrozone w momencie dodania? Owner: implementator. Block: no.
- **Risk:** Kod kreskowy jest pomocny, ale nie moze byc blokujaca integracja; fallback reczny utrzymuje scope MVP.
- **Status:** proposed

### S-03: Recipe Builder With Macro Calculation

- **Outcome:** user can build recipes from their products and see deterministic recipe macro totals.
- **Change ID:** recipe-builder-with-macro-calculation
- **PRD refs:** US-01, FR-007, FR-008, FR-009, FR-010
- **Prerequisites:** S-02
- **Parallel with:** -
- **Blockers:** -
- **Unknowns:**
  - Co dzieje sie z wpisami planu po usunieciu uzywanego przepisu? Owner: user. Block: no.
  - Czy skladniki przepisu sa wpisywane dla calego przepisu czy per porcja? Owner: implementator. Block: no.
- **Risk:** To pierwszy twardy test deterministycznego makro; blad tutaj zatruje plan dnia i liste zakupow.
- **Status:** proposed

### S-04: Daily Goals And Meal Plan

- **Outcome:** user can set daily macro goals and add recipes to a selected day by meal type and portions.
- **Change ID:** daily-goals-and-meal-plan
- **PRD refs:** US-01, FR-011, FR-012, FR-014
- **Prerequisites:** S-03
- **Parallel with:** -
- **Blockers:** -
- **Unknowns:**
  - Czy UI ma jasno pokazac, ze w MVP istnieje jeden aktualny zestaw celow, bez historii? Owner: implementator. Block: no.
- **Risk:** Ten slice laczy cele, porcje i typ posilku; przy low-complexity nie powinien jeszcze rozszerzac sie do planu tygodniowego.
- **Status:** proposed

### S-05: Daily Macro Summary

- **Outcome:** user can see kcal and macro totals for a selected day plus the numeric delta against their goals.
- **Change ID:** daily-macro-summary
- **PRD refs:** US-01, FR-013
- **Prerequisites:** S-04
- **Parallel with:** -
- **Blockers:** -
- **Unknowns:** -
- **Risk:** To zamyka kalibracyjna czesc reguly biznesowej; jesli liczby nie sa zaufane, lista zakupow tez nie bedzie wiarygodnym wynikiem planu.
- **Status:** proposed

### S-06: Shopping List From Day Plan

- **Outcome:** user can generate and view a deduplicated shopping list from the selected day plan.
- **Change ID:** shopping-list-from-day-plan
- **PRD refs:** US-01, US-03, FR-015, FR-016
- **Prerequisites:** S-05
- **Parallel with:** -
- **Blockers:** -
- **Unknowns:** -
- **Risk:** To domyka zakupowa czesc reguly biznesowej; sekwencja celowo czeka na plan i makro, bo lista ma wynikac z planu, nie z osobnego koszyka.
- **Status:** proposed

## Backlog Handoff

| Roadmap ID | Change ID | Suggested issue title | Ready for `/10x-plan` | Notes |
|---|---|---|---|---|
| F-01 | account-data-boundary | Prepare Supabase Auth token boundary and per-user data guard | yes | Run `/10x-plan account-data-boundary`; plan ASP.NET Core JWT validation, current-user abstraction, and user-scope tests. |
| F-02 | persistent-user-resources | Prepare Supabase Postgres persistence and deterministic calculation path | no | Depends on F-01; plan EF Core + Npgsql, migrations, user-owned tables, and secret handling. |
| F-03 | responsive-app-shell | Prepare responsive app shell for the first MVP flow | no | Depends on F-01. |
| S-01 | account-sign-in-flow | Let users register, sign in, sign out, and reach protected app | no | Depends on F-01 and F-03. |
| S-02 | product-catalog-with-barcode-fallback | Let users manage products with barcode fallback | no | Depends on S-01 and F-02. |
| S-03 | recipe-builder-with-macro-calculation | Let users build recipes and see macro totals | no | Depends on S-02. |
| S-04 | daily-goals-and-meal-plan | Let users set goals and plan meals for a day | no | Depends on S-03. |
| S-05 | daily-macro-summary | Show daily macro totals and goal deltas | no | Depends on S-04. |
| S-06 | shopping-list-from-day-plan | Generate a shopping list from the day plan | no | North star; depends on S-05. |

## Open Roadmap Questions

1. **Zachowanie zaleznych danych po usunieciu produktu/przepisu.** Owner: user. Block: no; affects S-02 and S-03 before first production deployment.
2. **Jednostki miary produktow (g / ml / szt).** Owner: user. Block: no; affects F-02, S-02, and S-06.
3. **Strategia danych po kodzie kreskowym.** Owner: implementator. Block: no; affects S-02.
4. **Granice gramatur w przepisie: per-przepis czy per-porcja.** Owner: implementator. Block: no; affects F-02 and S-03.
5. **Edycja celow dziennych - czy sa wersjonowane lub sa jednym aktualnym zestawem.** Owner: implementator. Block: no; affects S-04.

## Parked

- **Interfejs konsolowy w MVP** - Why parked: PRD Non-Goals; wraca po webowym MVP.
- **Tagi przepisow** - Why parked: PRD Non-Goals; filtrowanie nie blokuje pierwszego przeplywu.
- **Eksport listy zakupow do pliku** - Why parked: PRD Non-Goals; lista na ekranie wystarcza dla MVP.
- **Skaner kodu kreskowego kamera** - Why parked: PRD Non-Goals; MVP wpisuje kod recznie.
- **Plan tygodniowy / wielodniowy** - Why parked: PRD Non-Goals; pierwszy przeplyw dotyczy jednego dnia.
- **Spolecznosciowe feature'y** - Why parked: PRD Non-Goals; poza zakresem single-user MVP.
- **AI generujace plany dietetyczne** - Why parked: PRD Non-Goals; uzytkownik sam komponuje plan.
- **Spizarnia / no-waste / daty waznosci** - Why parked: PRD Non-Goals; nie blokuje listy zakupow z planu.
- **Aplikacja mobilna natywna** - Why parked: PRD Non-Goals; MVP jest responsywna aplikacja webowa.
- **Role administracyjne / dietetyk-pacjent** - Why parked: PRD Non-Goals; model rol jest plaski.
- **Historia celow / cykle** - Why parked: PRD Non-Goals; MVP ma jeden aktualny zestaw celow.
- **Rozbudowana edycja wpisow planu** - Why parked: PRD Non-Goals; FR-014 pokrywa minimum.
- **Compliance ponad podstawowy GDPR** - Why parked: PRD Non-Goals; niewspolmierne do skali MVP.
- **Offline-first** - Why parked: PRD Non-Goals; aplikacja wymaga polaczenia.
- **SLA uptime** - Why parked: PRD Non-Goals; MVP ma best-effort dostepnosc.
- **Formalna deklaracja WCAG-AA** - Why parked: PRD Non-Goals; uzytecznosc i kontrast bez formalnej deklaracji.

## Done
