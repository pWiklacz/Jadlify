# Raport analizy MVP — Jadlify

**Data sprawdzenia:** 2026-07-18
**Zasady analizy:** `.claude/prompts/mvp-check.md`
**Zakres:** ocena wyłącznie na podstawie kodu i dokumentacji w repozytorium — bez oceny warstwy wizualnej, stylów, dostępności ani faktu wdrożenia (poza zakresem).

**Typ projektu:** aplikacja webowa (SPA React/TypeScript + backend .NET 8 w architekturze Clean Architecture: Domain / Application / Infrastructure / API), z uwierzytelnianiem opartym o Supabase. Domena: planowanie posiłków z trzymaniem makroskładników i generowaniem listy zakupów.

---

## 1. Checklist — 5 kryteriów

### ✅ Kryterium 1: Operacje CRUD

**Status: SPEŁNIONE.** Pełny CRUD istnieje dla wielu typów zasobów i operuje na danych utrwalonych (EF Core + SQLite/Postgres). Najczystszy przykład — **Produkty**:

| Operacja | Dowód |
|---|---|
| **Create** | `ProductEndpoints.cs:51` `MapPost("/")` → `CreateProductCommandHandler` → `ProductRepository.AddAsync` (`ProductRepository.cs:23`) |
| **Read** | `ProductEndpoints.cs:26` `MapGet("/")` (lista) i `:41` `MapGet("/{id:guid}")` (pojedynczy) → `ListAsync`/`GetByIdAsync` |
| **Update** | `ProductEndpoints.cs:92` `MapPut("/{id:guid}")` → `UpdateProductCommandHandler` → `ProductRepository.UpdateAsync` (`:102`) — modyfikuje utrwaloną encję |
| **Delete** | `ProductEndpoints.cs:127` `MapDelete("/{id:guid}")` → `ProductRepository.DeleteAsync` (`:130`) |

Analogiczny, kompletny CRUD mają też **Przepisy** (`RecipeEndpoints.cs`), **Plan posiłków** (`MealPlanEndpoints.cs` — Add/List/Update/Delete) oraz **Cele dzienne** (Upsert/Get). Update działa na danych już zapisanych (nie jest to przejściowa edycja w UI), więc warunek jest spełniony z nawiązką.

---

### ✅ Kryterium 2: Logika biznesowa (ponad zwykły CRUD)

**Status: SPEŁNIONE.** Projekt zawiera dwie odrębne, nietrywialne reguły domenowe — dokładnie te, które PRD wskazuje jako unikalną wartość produktu:

- **Deterministyczna kalkulacja makroskładników** — `MacroCalculator.cs` (`src/Jadlify.Domain/Nutrition/`). Skaluje wartości proporcjonalnie do gramatury (`ForProductAmount`), sumuje przepis (`RecipeTotal`), liczy per-porcję (`RecipePerServing`), na wpis planu (`ForMealEntry`) i sumę dnia (`DayTotal`). To realne obliczenia na proporcji 100g, nie CRUD.
- **Agregacja listy zakupów** — `ShoppingListCalculator.cs` (`src/Jadlify.Domain/Shopping/`). Grupuje składniki **po `ProductId`** (nie po nazwie), skaluje gramatury współczynnikiem `entry.Portions / recipe.Portions` i sumuje powtórzenia produktu w jedną pozycję (`ShoppingListItemAccumulator`).

Obie realizują regułę biznesową opisaną w PRD §Business Logic (jeden plan → dwa wyjścia: makro vs cel oraz lista zakupów).

---

### ✅ Kryterium 3: Testy adresujące zdefiniowane ryzyko

**Status: SPEŁNIONE.** Istnieje formalny **plan testów** (`context/foundation/test-plan.md`) z jawną mapą ryzyk (§2 Risk Map), a konkretne testy mapują się na te ryzyka:

| Ryzyko z test-plan.md | Test, który je ćwiczy |
|---|---|
| **#1** Suma makro dnia rozjeżdża się z realną sumą (per-porcja vs per-przepis, ułamki, zaokrąglenia) | `MacroCalculatorTests.cs` — m.in. `RecipeTotal_WithFractionalGrams_MatchesIndependentOracle` (:175), `RecipePerServing_WithThreePortions_ProducesRepeatingDecimal` (:195), `DayTotal_WithMultipleDifferentRecipes_MatchesIndependentOracle` (:238) — z **niezależną wyrocznią** liczoną ręcznie (komentarze `// Oracle:`), co eliminuje tautologię |
| **#2** Lista zakupów ma duplikaty / złą sumę gramatur | `GetShoppingListQueryHandlerTests.cs::AggregatesDuplicateProductsAcrossEntries` (:33) — dowodzi sumowania 400g@2/4 + 100g@1/2 + 400g@1/4 = **350g** w jednej pozycji; plus `ShoppingListCalculatorTests.cs` |
| **#3** IDOR — dostęp do cudzego zasobu | `ProductEndpointsTests.cs::UserB_CannotAccessUserAProduct_Returns404` (:100) — GET/PUT/DELETE użytkownika B na zasobie A zwracają 404, zasób A pozostaje nienaruszony |
| **#4** Niezalogowany / niepoprawny JWT dosięga zasobu | `AuthBoundaryTests.cs` (anon→401, invalid→401, brak `sub`→403) + `AsymmetricJwtValidationTests.cs` |

Baza testów jest obszerna: 5 projektów xUnit (Domain/Application/Infrastructure/API.Tests) + Vitest/RTL na froncie (~14 speców). To znacznie przekracza minimum „przynajmniej jeden test odpowiadający zdefiniowanemu ryzyku".

---

### ✅ Kryterium 4: Uwierzytelnianie powiązane z użytkownikiem

**Status: SPEŁNIONE.** Uwierzytelnianie i pełne scope'owanie zasobów per-user:

- **Logowanie:** JWT Bearer walidowany asymetrycznie (ES256/JWKS) z Supabase — `Program.cs:33-73`. Globalna polityka fallback wymaga zalogowanego użytkownika z claimem `sub` (`Program.cs:75-79`), więc każdy endpoint domyślnie chroniony (brak `AllowAnonymous` poza `/health` i SPA fallback).
- **Zasoby przypisane do użytkownika:** tożsamość płynie przez `ICurrentUser` (`HttpContextCurrentUser`), a **każde** zapytanie repozytorium filtruje po właścicielu — np. `ProductRepository.cs:36-42, 50-53, 106-111, 132-137` (warunek `EF.Property<string>(..., UserIdProperty) == owner` w Get/List/Update/Delete). Zapis stempluje właściciela przy `AddAsync` (`:28`).
- **Weryfikacja izolacji:** potwierdzona testem cross-user (`UserB_CannotAccessUserAProduct_Returns404`).

To model wielo-użytkownikowy z prawdziwą izolacją danych — spełnia kryterium w najmocniejszej postaci (nawet nie korzystając z furtki „single-user").

---

### ✅ Kryterium 5: Dokumentacja

**Status: SPEŁNIONE.** Fundament 10x jest kompletny w `context/foundation/`:

- **PRD** (`prd.md`) — bogaty, bez placeholderów: wizja/problem, persona, kryteria sukcesu, 16 wymagań funkcjonalnych (FR-001…FR-016) z analizą sokratejską, NFR, logika biznesowa, kontrola dostępu, non-goals, otwarte pytania.
- **Plan testów** (`test-plan.md`), **roadmap** (`roadmap.md`), **tech-stack** (`tech-stack.md`), **shape-notes**, **infrastructure**, plus bogate archiwum zmian (`context/archive/*` z plan/change/review dla każdego slice'a).
- README frontendu (`src/Jadlify.Web/README.md`) — dokładny opis uruchomienia, Supabase, build/deploy. Dodatkowo `docs/jadlify-koncepcja-mvp.md`, `AGENTS.md`, `CLAUDE.md`.

Kryterium (README + PRD/dokument shape'ujący z realną treścią) jest spełnione. **Jedyna drobna uwaga:** brak README w katalogu głównym repozytorium — istnieje tylko README frontendu. Nie blokuje to kryterium (PRD i pozostały fundament w pełni opisują „co to jest, problem, zakres, funkcje"), ale to najtańsza możliwa poprawka „kosmetyczna" — patrz niżej.

---

## 2. Status projektu

| # | Kryterium | Status |
|---|---|:---:|
| 1 | Operacje CRUD | ✅ |
| 2 | Logika biznesowa | ✅ |
| 3 | Testy adresujące ryzyko | ✅ |
| 4 | Uwierzytelnianie per-user | ✅ |
| 5 | Dokumentacja | ✅ |

### Wynik: 5/5 = 100% — wszystkie minimalne wymagania techniczne spełnione.

---

## 3. Priorytetowe usprawnienia

Wszystkie kryteria spełnione, więc poniżej **tylko opcjonalne drobiazgi** (żadnego nie brakuje do zaliczenia progu):

- **README w katalogu głównym** *(najtańsza poprawka porządkowa)* — dodać `README.md` w root repo: 3–4 zdania „czym jest Jadlify", link do `context/foundation/prd.md` i do `src/Jadlify.Web/README.md`. Obecnie pierwsze, co widzi nowy odbiorca na GitHubie, to README frontendu, które nie tłumaczy produktu jako całości.

---

## 4. Uwaga: projekt wyraźnie wykracza ponad minimum

To nie jest „ledwo zaliczony" MVP — kilka sygnałów kwalifikuje go do wyróżnienia:

- **Dojrzała architektura** — Clean Architecture z rozdziałem Domain/Application/Infrastructure/API, wzorzec Result (`SharedKernel`), własny mediator z pipeline validation behaviors, walidatory FluentValidation dla każdej komendy.
- **Testowanie na poziomie inżynierskim** — plan testów oparty o macierz ryzyk (impact × likelihood), niezależne wyrocznie eliminujące tautologię, testy cross-user IDOR, walidacja asymetrycznego JWT, fault-injection na stubie Open Food Facts.
- **Odporność integracji zewnętrznej** — barcode lookup (Open Food Facts) z fallbackiem do ręcznego wpisu jako świadoma decyzja projektowa (FR-004 + guardrail), a nie blokada.
- **Snapshoty składników** — przepisy zapamiętują wartości makro produktu w momencie dodania, więc usunięcie produktu nie psuje historycznych sum (przemyślany kontrakt cross-slice).

Spełnienie 5/5 czyści próg techniczny, ale nie gwarantuje samo w sobie certyfikacji. Biorąc jednak pod uwagę powyższe, projekt ma solidne podstawy do ubiegania się o wyróżnienie / Demo Day.
