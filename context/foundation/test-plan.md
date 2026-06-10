# Test Plan

> Fazowy rollout testów dla tego projektu. Strategia zamrożona u góry
> (§1–§5); wzorce kucharskie na dole (§6) wypełniają się w miarę wdrażania faz.
> Przeczytaj przed napisaniem jakiegokolwiek nowego testu.
>
> Refresh: uruchom `/10x-test-plan --refresh`, gdy plan się zestarzeje (zob. §8).
>
> Last updated: 2026-06-10

## 1. Strategy

Testy w tym projekcie podlegają trzem nienegocjowalnym zasadom:

1. **Cost × signal.** Wygrywa najtańszy test dający realny sygnał dla danego
   ryzyka. Nie promuj do e2e dlatego, że "tak bezpieczniej". Nie nakładaj
   modelu wizyjnego na deterministyczny diff, który już łapie regresję.
2. **Głosy użytkownika to dowód pierwszej kategorii.** Ryzyka zakotwiczone w
   "zespół boi się X, a awaria ujawniłaby się gdzieś w obszarze <area>"
   ważą tyle samo, co linie PRD czy dane o churnie.
3. **Ryzyka to scenariusze, nie lokalizacje w kodzie.** Ten plan opisuje *co
   może zawieść* i *dlaczego uważamy to za prawdopodobne* — na podstawie
   dokumentów, wywiadu i *sygnału* z kodu (churn, struktura, baza testów).
   NIE twierdzi, że wie, która linia jest właścicielem awarii. Tę wiedzę
   produkuje `/10x-research` podczas każdej fazy rolloutu. Jeśli plan i
   research nie zgadzają się co do tego, gdzie żyje awaria — research jest
   prawdą bazową.

Hot-spot scope użyty do ważenia likelihood: `src/Jadlify.Web/src`,
`src/Jadlify.Infrastructure/Persistence`,
`src/Jadlify.Application/{Common,Planning,Recipes,Products}`,
`src/Jadlify.Domain/Nutrition`.

## 2. Risk Map

Najważniejsze scenariusze awarii, które ten projekt musi chronić,
uporządkowane wg ryzyka = impact × likelihood. Ryzyka to scenariusze
awarii w kategoriach użytkownika/biznesu, nie nazwy testów. Kolumna Źródło
cytuje *dowód, który wyniósł to ryzyko na wierzch* — nigdy konkretnego
pliku jako "gdzie żyje awaria" (to robota researchu, zob. §1 zasada #3).

| # | Ryzyko (scenariusz awarii) | Impact | Likelihood | Źródło (dowód — nie kotwica) |
|---|---|---|---|---|
| 1 | Suma makro dnia rozjeżdża się z realną sumą składników (zła konwencja per-porcja vs per-przepis, ułamkowe gramatury, zaokrąglenia) — użytkownik ufa złym liczbom | High | High | interview Q1; PRD NFR Determinizm kalkulacji + FR-008/FR-013; hot-spot `src/Jadlify.Application/Planning` (26 c/30d) i `src/Jadlify.Application/Recipes` (25 c/30d); PRD Open Question 4 (per-porcja vs per-przepis) |
| 2 | Lista zakupów ma duplikaty albo złą sumę gramatur (agregacja po produkcie zawodzi) | High | Medium | interview Q1; PRD US-03 / FR-015 (AC: "100g + 50g tego samego produktu = jeden wpis 150g, brak duplikatów") |
| 3 | IDOR — zalogowany użytkownik czyta lub modyfikuje cudzy zasób na którymś endpointcie (sprawdzane authn, nie ownership) | High | Medium | PRD Access Control + NFR Izolacja danych; abuse/authorization lens; wiele zasobów user-owned (produkty/przepisy/plan/cele/lista) |
| 4 | Niezalogowany ruch lub wygasły/niepoprawny JWT dosięga chronionego zasobu | High | Low | PRD Access Control ("Nieautentykowany dostęp: brak"); tech-stack ES256/JWKS; abuse/authn lens |
| 5 | Awaria, brak lub niepełne dane Open Food Facts blokują ręczne dodanie produktu zamiast fallbacku | Medium | Medium | PRD Guardrail "Odporność uzupełniania produktu po kodzie kreskowym" + FR-004; interview Q4 (OFF tylko stubowane) |
| 6 | Hasło lub token uwierzytelniający wycieka do logów albo error-body | High | Low | PRD NFR Prywatność operacyjna; abuse/secret-leakage lens |

**Rubryka Impact × Likelihood.**

| Ocena | Impact | Likelihood |
|---|---|---|
| High | utrata dostępu, danych lub pieniędzy; awaria publicznie widoczna | obszar zmieniany co tydzień lub już się tu sparzyliśmy |
| Medium | funkcja degraduje, istnieje obejście, dotyka część użytkowników | dotykane okazjonalnie, bywało źródłem bugów |
| Low | kosmetyka, łatwo cofalne, bez efektu na danych | stabilny kod, rzadko ruszany |

Kolejność wg impact × likelihood. Chroń High × High najpierw. Ryzyko #4 i
#6 mają wysoki impact, ale niskie likelihood (istnieją już testy
asymetrycznego JWT i granicy auth) — pozostają w mapie, bo są tanio
testowalne deterministycznie, a nie tylko sprawą obserwowalności.

### Risk Response Guidance

| Ryzyko | Co dowodzi ochrony | Trzeba zakwestionować | Kontekst, który `/10x-research` musi ugruntować | Najtańsza warstwa | Anty-wzorzec |
|---|---|---|---|---|---|
| #1 | Makro dnia równe niezależnie policzonej sumie (oracle z proporcji 100g, nie z kodu) dla gramatur ułamkowych i wielu porcji | "przepis na 4 porcje, 150g = 150g/porcję czy 150g łącznie?" — konwencja musi być jawnie sprawdzona | konwencja gramatura per-przepis/per-porcja; zaokrąglenia; typ liczbowy używany w warstwie persistence | unit (Domain) + integration (Application/Planning) | oracle skopiowany z implementacji (tautologia); tylko happy-path całkowitych gramatur |
| #2 | Dwa przepisy z tym samym produktem dają jeden wpis z sumą gramatur; brak duplikatów | "agregacja po nazwie" zamiast po identyfikatorze produktu | klucz agregacji; zachowanie przy tym samym produkcie pochodzącym z różnych przepisów | integration (Application/Shopping) | asercja przepisana z kodu agregacji; pojedynczy przepis zamiast kolizji produktów |
| #3 | Użytkownik A nie odczyta ani nie zmieni zasobu użytkownika B na ŻADNYM endpointcie (404/403, nie 200) | "AuthBoundaryTests pokrywają wszystko" — sprawdzić macierz wszystkich zasobów user-owned | pełna lista user-owned endpointów; gdzie egzekwowany jest scope (handler vs repozytorium) | integration (API) — macierz IDOR | test tylko authn ("zalogowany → 200") bez weryfikacji cross-user ownership |
| #4 | Brak, wygasły lub obcego-issuera token daje 401 na chronionym zasobie; anon nie wchodzi | "ważny podpis = ważny dostęp" — sprawdzić issuer/audience/exp | walidacja JWKS, issuer/audience, ścieżka anonimowej powierzchni | integration (API) | mockowanie całej warstwy auth zamiast realnej walidacji tokenu |
| #5 | Gdy OFF zwraca błąd, `status:0` lub dane częściowe, użytkownik nadal zapisuje produkt ręcznie | "brak danych = błąd" zamiast gałęzi fallback do ręcznego wpisu | kontrakt adaptera OFF; mapowanie status:0/timeout/dane-częściowe na fallback | integration na stubie z fault-injection (NIE realna sieć) | realne wywołanie sieciowe OFF w teście (zob. §7); test tylko happy-path lookup |
| #6 | Logi i error-body nie zawierają tokenu ani hasła przy błędzie uwierzytelniania | "framework i tak tego nie loguje" | gdzie logowane są żądania i błędy; czy token trafia do trace | unit/integration (API, asercja na zawartości logu) | sprawdzenie tylko statusu odpowiedzi bez inspekcji logu |

## 3. Phased Rollout

Każdy wiersz to osobna faza rolloutu, która otworzy własny folder zmiany
przez `/10x-new`. Status przesuwa się od lewej do prawej; orkiestrator
aktualizuje Status, gdy artefakty pojawiają się na dysku.

| # | Phase name | Goal (one line) | Risks covered | Test types | Status | Change folder |
|---|---|---|---|---|---|---|
| 1 | Determinizm obliczeń i agregacji | Udowodnić #1 i #2 najtańszą warstwą z niezależnym oracle | #1, #2 | unit + integration | researched | context/changes/testing-determinism-and-aggregation/ |
| 2 | Granica izolacji i auth | Pełna macierz IDOR plus granica tokenu na wszystkich zasobach user-owned | #3, #4 | integration | not started | — |
| 3 | E2E krytycznego przepływu US-01 | Jeden test przez prawdziwy stack (API↔DB↔SPA) spinający promesę PRD | #1, #2, #3 | e2e | not started | — |
| 4 | Odporność integracji zewnętrznej i higiena sekretów | Fallback OFF na stubie z fault-injection; brak sekretów w logach | #5, #6 | integration | not started | — |
| 5 | Wpięcie quality gates | Zablokować podłogę w CI (lint, typecheck, unit+integration, e2e) | cross-cutting | gates | not started | — |

**Status vocabulary** (fixed — parser literals):

| Value | Meaning |
|---|---|
| `not started` | No change folder for this rollout phase yet. |
| `change opened` | `context/changes/<id>/` exists with `change.md`; research not done. |
| `researched` | `research.md` exists in the change folder. |
| `planned` | `plan.md` exists with a `## Progress` section. |
| `implementing` | Progress section has at least one `[x]` and at least one `[ ]`. |
| `complete` | Progress section is fully `[x]`. |

## 4. Stack

Klasyczna baza testów tego projektu. Profil: **`meaningful`** — 4 projekty
xUnit (`Jadlify.API/Application/Domain/Infrastructure.Tests`, ~48 plików)
plus frontendowy Vitest + RTL (~14 speców). Istniejące pokrycie obejmuje
m.in. granicę auth, walidację asymetrycznego JWT, user-scope, kalkulator
makro oraz wszystkie handlery, walidatory i endpointy. **Brak warstwy e2e.**

| Layer | Tool | Version | Notes |
|---|---|---|---|
| unit + integration (backend) | xUnit | — | 4 projekty `tests/Jadlify.*.Tests`; uruchamiane przez `pwsh ./.scripts/test-min.ps1` |
| integration (API) | ASP.NET `WebApplicationFactory` (`TestApiFactory`) | — | `tests/Jadlify.API.Tests/Common/TestApiFactory.cs`; auth stubowane przez `TestAuthenticationHandler` |
| unit + integration (frontend) | Vitest + React Testing Library | — | `src/Jadlify.Web`; `npm test` |
| API mocking (zewn. brzeg) | stub adaptera (`StubBarcodeProductLookup` / `FakeBarcodeProductLookup`) | — | OFF mockowany na granicy adaptera; brak realnej sieci w testach |
| e2e | none yet — see §3 Phase 3 | — | brak `playwright.config`; przepływ US-01 nie jest ćwiczony przez pełny stack |
| quality gates (CI) | none yet — see §3 Phase 5 | — | lokalnie min-scripty istnieją; egzekwowanie w CI do wpięcia |

**Stack grounding tools (current session):**
- Docs: context7 — dostępny, do walidacji aktualnego setupu Playwright/.NET przy fazie e2e; nie odpytany jeszcze; checked: 2026-06-10
- Search: Exa.ai — dostępny, do sprawdzenia aktualnego statusu narzędzi e2e/.NET; nie użyty; checked: 2026-06-10
- Runtime/browser: Claude Preview + Chrome MCP — dostępne; możliwa warstwa weryfikacji przy §3 Phase 3 (e2e), używać tylko gdy daje sygnał ponad deterministyczny test; checked: 2026-06-10
- Provider/platform: Supabase (CLI stack lokalny wg `supabase/config.toml`) — istotne dla auth/JWKS w fazach 2 i 3; checked: 2026-06-10

## 5. Quality Gates

| Gate | Where | Required? | Catches |
|---|---|---|---|
| lint + typecheck | local + CI | required | dryf składniowy / typów (backend `.editorconfig`/`Directory.Build.props`, frontend `npm run lint`) |
| unit + integration | local + CI | required after §3 Phase 1 | regresje logiki (makro, agregacja, izolacja) |
| e2e on critical flows | CI on PR | required after §3 Phase 3 | zepsuty krytyczny przepływ US-01 |
| post-edit hook | local (agent loop) | recommended after §3 Phase 5 | regresje w czasie edycji |
| visual diff (deterministic) | CI on PR | optional | regresje renderowania (selektywnie, nie każdy ekran) |
| pre-prod smoke | between merge + prod | optional | awarie specyficzne dla środowiska |

## 6. Cookbook Patterns

Jak dodawać nowe testy w tym projekcie. Każda podsekcja wypełnia się, gdy
odpowiednia faza rolloutu wyląduje; wcześniej brzmi "TBD — see §3 Phase <N>".

### 6.1 Adding a unit test

- TBD — see §3 Phase 1 (wzorzec: niezależny oracle dla makro per-100g; gramatury ułamkowe i wiele porcji).

### 6.2 Adding an integration test

- TBD — see §3 Phase 1 i Phase 2 (wzorzec: agregacja listy zakupów po id produktu; macierz IDOR przez `TestApiFactory`).

### 6.3 Adding an e2e test

- TBD — see §3 Phase 3 (wzorzec: pełny przepływ US-01 przez API↔DB↔SPA).

### 6.4 Adding a test for a new API endpoint

- TBD — see §3 Phase 2 (wzorzec: integration przez `WebApplicationFactory`/`TestApiFactory`; asercja request → response ORAZ ownership cross-user; mock tylko zewnętrznego brzegu HTTP).

### 6.5 Adding a test for external-integration fallback

- TBD — see §3 Phase 4 (wzorzec: fault-injection na stubie OFF; mapowanie błąd/status:0/partial na ścieżkę ręczną).

### 6.6 Per-rollout-phase notes

(Opcjonalne. Po wylądowaniu fazy `/10x-implement` dopisuje tu 2–3 linie o tym, czego faza nauczyła.)

## 7. What We Deliberately Don't Test

Wykluczenia uzgodnione podczas rolloutu (wywiad Faza 2, Q5). Przyszli
kontrybutorzy powinni je respektować, dopóki założenie się nie zmieni.

- **Snapshoty statycznego UI / layoutu** — kruche, łapią mało; regresje wizualne adresujemy selektywnie deterministycznym diffem, nie snapshotem. Re-evaluate jeśli pojawi się złożony, dynamiczny widok wrażliwy na renderowanie. (Source: interview Q5.)
- **Realne sieciowe wywołania Open Food Facts w CI** — flaky i zależne od zewnętrznego API; testujemy kontrakt/fallback na stubie z fault-injection (§3 Phase 4). Re-evaluate jeśli adapter OFF zostanie wymieniony na inne źródło danych. (Source: interview Q5.)
- **Trywialny CRUD i wylogowanie bez logiki** — niski blast radius, brak reguły biznesowej do złamania. Re-evaluate jeśli dojdzie tam logika (np. kaskady przy usuwaniu produktu/przepisu — PRD Open Question 1). (Source: interview Q5.)
- **Warstwa AI-native (vision/LLM-review)** — produkt to deterministyczna matematyka makro, izolacja danych i agregacja; review wizyjny/LLM nie daje taniego sygnału ponad deterministyczne asercje. Re-evaluate jeśli pojawi się powierzchnia trudno-deterministyczna lub generatywna. (Source: challenger pass, cost × signal.)

## 8. Freshness Ledger

- Strategy (§1–§5) last reviewed: 2026-06-10
- Stack versions last verified: 2026-06-10
- AI-native tool references last verified: 2026-06-10

Refresh (`/10x-test-plan --refresh`) when:

- a new top-3 risk surfaces from the roadmap or archive,
- a recommended tool's `checked:` date is older than three months,
- the project's tech stack changes (new framework, new test runner),
- §7 negative-space no longer matches what the team believes.
