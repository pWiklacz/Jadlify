---
title: Domain Distillation Map — Jadlify
created: 2026-08-04
type: domain-distillation
---

# Mapa Destylacji Domeny — Jadlify (Domain Distillation Map)

Niniejszy dokument prezentuje rezultaty analizy Domain-Driven Design (DDD) przeprowadzonej na podstawie dokumentacji projektowej (`context/foundation/prd.md`, `context/foundation/tech-stack.md`, `context/foundation/shape-notes.md`, `docs/jadlify-koncepcja-mvp.md`) oraz kodu źródłowego solucji `Jadlify.slnx`.

---

## KROK 0 — Kontekst Projektu i Architektura

### 1. Dokumenty Źródłowe
Analiza została oparta na kompletnym zestawie dokumentów wymagań i wizji:
- [prd.md](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md) — Główny dokument PRD określający wizję, cele sukcesu (Success Criteria), ograniczenia (Guardrails), funkcjonalności (FR-001 do FR-016), wymagania jakościowe (NFR) oraz Non-Goals.
- [jadlify-koncepcja-mvp.md](file:///c:/Users/wikla/source/repos/Jadlify/docs/jadlify-koncepcja-mvp.md) — Pierwotna koncepcja produktu, opisująca intencję biznesową i architekturę z perspektywy autora.
- [tech-stack.md](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/tech-stack.md) — Ustalone decyzje stosu technologicznego i integracji tożsamości.
- [shape-notes.md](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/shape-notes.md) — Szczegółowe zapiski z fazy kształtowania (shaping).

### 2. Stos Technologiczny i Izolacja Tożsamości
- **Backend**: .NET 10 (ASP.NET Core Web API), C# 13.
- **Baza Danych**: Supabase PostgreSQL z dostępem przez EF Core (`Npgsql.EntityFrameworkCore.PostgreSQL`).
- **Autentykacja**: Supabase Auth (weryfikacja asymetrycznych tokenów JWT ES256 w API; rola `sub` mapowana na identyfikator użytkownika `ApplicationUserId`).
- **Frontend**: React + Vite + TypeScript w `src/Jadlify.Web` (serwowany jako SPA z `wwwroot` w API).

### 3. Architektura i Pakiety Źródłowe (`src/`)
Aplikacja realizuje zasady Czystej Architektury (Clean / Onion Architecture) z podziałem na warstwy:
- `src/Jadlify.Domain` — Warstwa domeny biznesowej. Zawiera encje, obiekty wartości (Value Objects) oraz serwisy domenowe. Brak jakichkolwiek zależności do innych projektów solucji.
  - Subkatalogi: `Nutrition`, `Planning`, `Products`, `Recipes`, `Shopping`.
- `src/Jadlify.Application` — Warstwa przypadków użycia. Obsługuje komendy/zapytania (wzorzec Mediator), DTO, walidacje (`ValidationBehavior`) oraz kontekst tożsamości (`UserScope`, `ICurrentUser`).
  - Subkatalogi: `Common`, `Identity`, `Planning`, `Products`, `Recipes`, `Shopping`.
- `src/Jadlify.Infrastructure` — Warstwa persystencji i integracji zewnętrznych. Konfiguracje EF Core (`JadlifyDbContext`), repozytoria, migracje bazy danych oraz klient API Open Food Facts (`OpenFoodFactsBarcodeLookup`).
- `src/Jadlify.API` — Warstwa prezentacji HTTP (Minimal APIs). Definiuje endpointy REST (`MapProductEndpoints`, `MapRecipeEndpoints`, `MapDailyGoalEndpoints`, `MapMealPlanEndpoints`, `MapShoppingListEndpoints`, `MapShoppingListsEndpoints`) oraz polityki autoryzacji.
- `src/Jadlify.SharedKernel` — Wspólne typy bazowe i abstrakcje krzyżowe (`Result`, `Error`).

---

## KROK 1 — Ubiquitous Language (Język Wszechobecny)

Poniższa tabela zestawia kluczowe pojęcia domenowe zdefiniowane w dokumentacji biznesowej z ich odwzorowaniem w kodzie źródłowym.

| Pojęcie Domenowe | Definicja Biznesowa | Cytat Źródłowy (Dokument) | Odpowiednik w Kodzie |
| :--- | :--- | :--- | :--- |
| **Produkt (Product)** | Bazowy artykuł spożywczy zawierający nazwę, wartości odżywcze przeliczone na 100g, opcjonalny kod kreskowy, markę, kategorię oraz masę opakowania. | [prd.md:112](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L112) *"dodać produkt ręcznie z nazwą i wartościami odżywczymi na 100g"* | [Product.cs:5](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Products/Product.cs#L5), [ProductConfiguration.cs:11](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Infrastructure/Persistence/Configurations/ProductConfiguration.cs#L11) |
| **Wartości Odżywcze (MacroNutrients)** | Niemodyfikowalny zestaw parametrów kalorycznych i makroskładników: kalorie (kcal), białko, tłuszcz i węglowodany. | [prd.md:59](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L59) *"wynik liczenia kcal/białka/tłuszczu/węglowodanów jest zawsze ten sam"* | [MacroNutrients.cs:3](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Nutrition/MacroNutrients.cs#L3), [MacroCalculator.cs:7](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Nutrition/MacroCalculator.cs#L7) |
| **Wprowadzanie po Kodzie (Barcode Lookup / Fallback)** | Pobranie wstępnych danych produktu z zewnętrznej bazy (Open Food Facts) po kodzie EAN z bezwzględnym fallbackiem do ręcznego formularza przy braku danych. | [prd.md:60](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L60) *"gdy dane dla kodu są niedostępne... użytkownik nadal może dodać produkt ręcznie"* | [IBarcodeProductLookup.cs:7](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Application/Products/IBarcodeProductLookup.cs#L7), [OpenFoodFactsBarcodeLookup.cs:16](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Infrastructure/OpenFoodFacts/OpenFoodFactsBarcodeLookup.cs#L16) |
| **Przepis (Recipe)** | Kompozycja posiłku składająca się z nazwy, zadeklarowanej liczby porcji oraz listy składników z podanymi gramaturami na cały przepis. | [prd.md:122](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L122) *"stworzyć przepis składający się ze składników (produkt + gramatura) i liczby porcji"* | [Recipe.cs:3](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Recipes/Recipe.cs#L3), [RecipeConfiguration.cs:11](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Infrastructure/Persistence/Configurations/RecipeConfiguration.cs#L11) |
| **Składnik Przepisu (RecipeIngredient)** | Element przepisu łączący referencję do produktu z jego gramaturą dla całego przepisu oraz zamrożoną migawką nazwy i makro na 100g. | [prd.md:72](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L72) *"Wartości makro... odpowiadają sumie składników z każdego przepisu w planie"* | [RecipeIngredient.cs:5](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Recipes/RecipeIngredient.cs#L5) |
| **Porcja Przepisu (Portions / RecipePortions)** | Jednostka podziału przepisu. W planie posiłków porcje przepisów są deklarowane w dodatnich krokach co 0.5 porcji. | [prd.md:123](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L123) *"FR-008 liczy makro per-porcję i sumarycznie; w planie posiłków user może wskazać ile porcji bierze"* | [MealPlanEntry.cs:16](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Planning/MealPlanEntry.cs#L16) (`PortionStep = 0.5m`), [Recipe.cs:32](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Recipes/Recipe.cs#L32) |
| **Plan Posiłków (MealPlan / MealPlanEntry)** | Przypisanie posiłku do konkretnego dnia i typu posiłku. Może wskazywać przepis (w porcjach) lub bezpośrednio produkt (w gramach). | [prd.md:137](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L137) *"dodać przepis do planu wybranego dnia, wskazując typ posiłku... oraz liczbę porcji"* | [MealPlanEntry.cs:10](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Planning/MealPlanEntry.cs#L10) *(Uwaga: Brak klasy `MealPlan` stanowiącej agregat dnia!)* |
| **Typ Posiłku (MealType)** | Kategoria posiłku w ciągu dnia: Śniadanie (Breakfast), Obiad (Lunch), Kolacja (Dinner), Przekąska (Snack). | [prd.md:137](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L137) *"wskazując typ posiłku (śniadanie/obiad/kolacja/przekąska)"* | [MealType.cs:3](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Planning/MealType.cs#L3) |
| **Migawka Produktu w Planie (PlannedProductSnapshot)** | Zamrożona w momencie tworzenia wpisu planu kopia danych odżywczych i kategorii produktu planowanego bezpośrednio (poza przepisami). | [prd.md:215](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L215) *(Zasada niezależności danych i historii)* | [PlannedProductSnapshot.cs:13](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Planning/PlannedProductSnapshot.cs#L13) |
| **Cel Dzienny (DailyMacroGoal / DailyNutritionTarget)** | Ustawiony przez użytkownika cel wartości odżywczych na dany dzień, stanowiący punkt punktacji różnicy (MacroRemaining). | [prd.md:132](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L132) *"ustawić swoje dzienne cele kaloryczne (kcal, białko, tłuszcz, węglowodany)"* | [DailyMacroGoal.cs:5](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Planning/DailyMacroGoal.cs#L5), [DailyMacroGoalConfiguration.cs:9](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Infrastructure/Persistence/Configurations/DailyMacroGoalConfiguration.cs#L9) |
| **Lista Zakupów (ShoppingList)** | Trwały agregat reprezentujący zagregowane zapotrzebowanie na składniki z wybranych dni planu z odznaczaniem pozycji kupionych. | [prd.md:146](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L146) *"wygenerować listę zakupów z planu (jeden dzień), z agregacją tych samych produktów"* | [ShoppingList.cs:16](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Shopping/ShoppingList.cs#L16), [ShoppingListConfiguration.cs:12](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Infrastructure/Persistence/Configurations/ShoppingListConfiguration.cs#L12) |
| **Agregacja Składników (ShoppingListCalculator)** | Algorytm sumowania gramatur tych samych produktów pochodzących z różnych przepisów i bezpośrednich wpisów planu. | [prd.md:97](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L97) *"Dwa przepisy używające 100g i 50g tego samego produktu produkują na liście jeden wpis z 150g"* | [ShoppingListCalculator.cs:36](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Shopping/ShoppingListCalculator.cs#36) |
| **Odbicie i Odcisk Planu (ShoppingListDiff / SourceFingerprint)** | Mechanizm wykrywania i podglądu zmian zachodzących w planie posiłków względem wygenerowanej aktywnej listy zakupów. | Rozszerzenie koncepcyjne po PRD (brak bezpośredniego zapisu w prd.md) | [ShoppingListDiff.cs:1](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Shopping/ShoppingListDiff.cs#L1), [ShoppingList.cs:45](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Shopping/ShoppingList.cs#L45) |
| **Tag Przepisu (Tag / RecipeTag)** | Etykiety charakteryzujące przepisy (np. `wege`, `bez cukru`, `meal prep`). | [jadlify-koncepcja-mvp.md:104](file:///c:/Users/wikla/source/repos/Jadlify/docs/jadlify-koncepcja-mvp.md#L104), przeniesione do Non-Goals w [prd.md:191](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L191) | **BRAK w kodzie** *(świadomie wycięte z zakresu MVP w PRD)* |
| **Izolacja Danych (UserScope / ApplicationUserId)** | Bezwarunkowa izolacja wszystkich zasobów domenowych per użytkownik zalogowany tokenem JWT Supabase. | [prd.md:58](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L58) *"zasoby jednego użytkownika nigdy nie są widoczne dla innego użytkownika"* | [UserScope.cs:9](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Application/Identity/UserScope.cs#L9), [ApplicationUserId.cs:3](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Application/Identity/ApplicationUserId.cs#L3) |

---

## KROK 2 — Klasyfikacja Subdomen

Subdomeny projektu zostały sklasyfikowane według ich wpływu na realizację celów biznesowych zawartych w wizji produktu ([prd.md:20-25](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L20-L25)) oraz kryteriów sukcesu ([prd.md:34-45](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L34-L45)).

| Obszar / Subdomena | Klasyfikacja | Uzasadnienie Biznesowe (Odniesienie do Wizji i PRD) |
| :--- | :--- | :--- |
| **Planowanie Posiłków i Bilansowanie Makroskładników** *(Meal Planning & Macro Balancing)* | **CORE** (Rdzeń) | Stanowi główny insight i unikalną wartość produktu ([prd.md:24](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L24)): deklaratywne zaplanowanie posiłków sprawia, że cele makro trafiają się same, a kalkulacje są automatyczne i deterministyczne. |
| **Generowanie i Synchronizacja Listy Zakupów** *(Shopping List Generation & Reconciliation)* | **CORE** (Rdzeń) | Zapewnia rozwiązanie drugiego głównego punktu tarcia użytkownika ([prd.md:22](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L22)): automatyczne przejście z planu dnia/wielodniowego do zagregowanej listy produktów bez duplikatów i z obsługą różnic w planie (`ShoppingListDiff`). |
| **Katalog Produktów i Wartości Odżywczych** *(Product Catalog & Nutrition Basis)* | **SUPPORTING** (Wspierająca) | Niezbędny budulec dla przepisów i planu ([prd.md:112](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L112)). Sama baza wartości odżywczych nie stanowi przewagi rynkowej (istnieją powszechne rejestry), ale jest krytyczna dla działania systemu. |
| **Komponowanie i Modyfikacja Przepisów** *(Recipe Management)* | **SUPPORTING** (Wspierająca) | Pozwala na wielokrotne użycie zestawu składników w planach posiłków oraz obsługuje scenariusze *meal prep* z skalowaniem porcji ([prd.md:122-124](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L122-L124)). |
| **Import z Zewnętrznych Baz Produktów** *(Barcode Lookup / Open Food Facts)* | **GENERIC** (Ogólna) | Funkcja pomocnicza usprawniająca wprowadzanie danych ([prd.md:114](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L114)). Wykorzystuje standardowe zewnętrzne API HTTP; brak danych z API nie blokuje działania systemu (odporność z fallbackiem). |
| **Tożsamość i Autentykacja Użytkownika** *(Authentication & Identity Isolation)* | **GENERIC** (Ogólna) | Standardowy mechanizm uwierzytelniania w pełni wydelegowany do Supabase Auth ([tech-stack.md:30](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/tech-stack.md#L30)). Zapewnia realizację Guardrailu izolacji danych ([prd.md:58](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L58)). |

---

## KROK 3 — Kandydaci na Agregaty i Niezmienniki Biznesowe

Analiza struktur w `src/Jadlify.Domain` wykazuje obecność 4 głównych kandydatów na agregaty oraz serwisy domenowe.

### 1. Agregat: `Recipe` (Przepis)
- **Struktura**: Root `Recipe` + encje potomne `RecipeIngredient`.
- **Niezmiennik 1**: Przepis musi posiadać dodatnią liczbę porcji (`Portions > 0`).
  - *Cytat*: [prd.md:122](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L122) *"liczby porcji, na którą przepis jest skalowany"*
  - *Status w kodzie*: **EGZEKWOWANY** — [Recipe.cs:10](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Recipes/Recipe.cs#L10) oraz [Recipe.cs:52](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Recipes/Recipe.cs#L52) rzucają `ArgumentOutOfRangeException`.
- **Niezmiennik 2**: Składniki wewnątrz przepisu muszą być unikalne po `ProductId` (brak zduplikowanych produktów w jednym przepisie).
  - *Cytat*: [docs/jadlify-koncepcja-mvp.md:102](file:///c:/Users/wikla/source/repos/Jadlify/docs/jadlify-koncepcja-mvp.md#L102) *"tworzenie przepisu z produktów"*
  - *Status w kodzie*: **EGZEKWOWANY** — [Recipe.cs:40-44](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Recipes/Recipe.cs#L40-L44) oraz [Recipe.cs:61-70](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Recipes/Recipe.cs#L61-L70) rzucają `InvalidOperationException` przy wykryciu duplikatu `ProductId`.
- **Niezmiennik 3**: Przepis musi zawierać co najmniej jeden składnik.
  - *Cytat*: [prd.md:122](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L122) *"składający się ze składników"*
  - *Status w kodzie*: **EGZEKWOWANY** — [Recipe.cs:56-59](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Recipes/Recipe.cs#L56-L59) sprawdzają `replacement.Count == 0` i rzucają wyjątek.

### 2. Agregat: `ShoppingList` (Lista Zakupów)
- **Struktura**: Root `ShoppingList` + `ShoppingListItem` + `ShoppingListSourceDay`.
- **Niezmiennik 1**: Zakończona lista zakupów (`Status == ShoppingListStatus.Completed`) jest immutable — żadna pozycja nie może być odznaczona ani zaktualizowana.
  - *Cytat*: [docs/jadlify-koncepcja-mvp.md:117](file:///c:/Users/wikla/source/repos/Jadlify/docs/jadlify-koncepcja-mvp.md#L117)
  - *Status w kodzie*: **EGZEKWOWANY** — [ShoppingList.cs:175-178](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Shopping/ShoppingList.cs#L175-L178) (`EnsureActive`) rzuca `InvalidOperationException("A completed shopping list is immutable.")`.
- **Niezmiennik 2**: Agregacja pozycji eliminuje duplikaty produktów i sumuje gramatury ze wszystkich posiłków źródłowych.
  - *Cytat*: [prd.md:97](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L97) *"Dwa przepisy używające 100g i 50g tego samego produktu produkują na liście jeden wpis z 150g"*
  - *Status w kodzie*: **EGZEKWOWANY** — [ShoppingListCalculator.cs:41](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Shopping/ShoppingListCalculator.cs#L41) agreguje po `ProductId`.
- **Niezmiennik 3**: Użytkownik posiada co najwyżej jedną aktywną listę zakupów w danym czasie.
  - *Cytat*: [prd.md:146](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L146)
  - *Status w kodzie*: **DEKLAROWANY W APLIKACJI** — Weryfikowany w warstwie aplikacji w [CreateShoppingListCommandHandler.cs:45](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Application/Shopping/ShoppingLists/CreateShoppingList/CreateShoppingListCommandHandler.cs#L45); agregat domeny nie ma wiedzy o innych listach.

### 3. Encja / Agregat: `MealPlanEntry` (Wpis Planu Posiłków)
- **Struktura**: Samodzielna encja `MealPlanEntry` zawierająca typ posiłku, datę, wielkość oraz wariant źródła (`Recipe` lub `Product`).
- **Niezmiennik 1**: Porcje przepisu w wpisie planu muszą być dodatnią wielokrotnością 0.5 porcji (`portions > 0` oraz `portions % 0.5 == 0`).
  - *Cytat*: [prd.md:137](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L137)
  - *Status w kodzie*: **EGZEKWOWANY** — [MealPlanEntry.cs:143-149](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Planning/MealPlanEntry.cs#L143-L149) sprawdzają `portions % PortionStep != 0m` i rzucają `ArgumentOutOfRangeException`.
- **Niezmiennik 2**: Wpis ma dokładnie jedno wykluczające się źródło: odnośnik do przepisu (`RecipeId`) mierzonego porcjami LUB migawkę produktu (`PlannedProductSnapshot`) mierzonego gramami.
  - *Cytat*: [prd.md:137](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L137)
  - *Status w kodzie*: **EGZEKWOWANY** — Metody statyczne fabrykujące [MealPlanEntry.cs:68](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Planning/MealPlanEntry.cs#L68) (`ForRecipe`) oraz [MealPlanEntry.cs:85](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Planning/MealPlanEntry.cs#L85) (`ForProduct`) gwarantują spójność wariantu.

### 4. Agregat: `Product` (Produkt)
- **Struktura**: Root `Product` + `MacroNutrients` + `NutritionFacts`.
- **Niezmiennik 1**: Produkt musi posiadać niepustą nazwę oraz nieujemne makroskładniki na 100g.
  - *Cytat*: [prd.md:112](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L112)
  - *Status w kodzie*: **EGZEKWOWANY** — Walidacja nazwy w [Product.cs:25](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Products/Product.cs#L25) oraz nieujemności w [MacroNutrients.cs:7-10](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Nutrition/MacroNutrients.cs#L7-L10).

---

## KROK 4 — Lista Rozjazdów: MODEL (Biznes) vs KOD (Realizacja)

W poniższej tabeli zestawiono najważniejsze obszary, w których założenia dokumentacji biznesowej i PRD różnią się od faktycznej implementacji w kodzie.

| Wymaganie / Model Biznesowy (Dokument) | Stan Faktyczny w Kodzie (Realizacja) | Dowód w Kodzie (Plik:Linia) | Wpływ Biznesowy / Ryzyko |
| :--- | :--- | :--- | :--- |
| **Brak Agregatu Dnia Planu (`MealPlan`)**: PRD ([prd.md:278](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L278)) oraz Koncepcja ([jadlify-koncepcja-mvp.md:263](file:///c:/Users/wikla/source/repos/Jadlify/docs/jadlify-koncepcja-mvp.md#L263)) jawnie definiują relację `User 1 - N MealPlan` i `MealPlan 1 - N MealPlanEntry`. Dzień posiłków miał stanowić spójny agregat. | W kodzie domenowym **NIE MA** klasy `MealPlan` ani `MealPlanDay`. Dzień jest jedynie luźnym atrybutem `DateOnly Date` w płaskiej encji `MealPlanEntry`. Obliczenia dnia wykonuje aplikacja. | [MealPlanEntry.cs:10](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Planning/MealPlanEntry.cs#L10), brak pliku `MealPlan.cs` w `src/Jadlify.Domain/Planning/` | **WYSOKI**: Brak spójności transakcyjnej dnia; zapytania aplikacyjne same składają podsumowania dnia. |
| **Obsługa Usunięcia Przepisu w Planie**: PRD (Open Question #1 w [prd.md:210](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L210)) pyta o spójność planu po usunięciu przepisu. FR-010 dopuszcza usunięcie przepisu. | Usunięcie przepisu kasuje go z bazy (`RecipeRepository.cs`), pozostawiając sieroty w `MealPlanEntry.RecipeId`. Przy obliczaniu podsumowania dnia, kod **cicho pomija** nieodnalezione przepisy! | [MacroCalculator.cs:64](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Nutrition/MacroCalculator.cs#L64) *"Callers drop entries whose recipe could not be resolved"*, [GetDailyMacroSummaryQueryHandler.cs:51](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Application/Planning/DailyMacroSummary/GetDailyMacroSummaryQueryHandler.cs#L51) | **ŚREDNI/WYSOKI**: Usunięcie przepisu cicho obniża wyliczone makro dnia użytkownika bez żadnego ostrzeżenia w UI. |
| **Persystencja i Wersjonowanie Listy Zakupów**: PRD ([prd.md:146-149](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L146-L149), FR-015/016) zakładało jednodniową listę zakupów tylko do odczytu na ekranie, bez zapisywania w bazie danych. | Kod w domenie zrealizował znacznie zaawansowany trwały Agregat `ShoppingList` z wersjonowaniem optymistycznym, cyfrowym odciskiem planu (`SourceFingerprint`), porównywaniem różnic (`ShoppingListDiff`) i historią (`Completed`). | [ShoppingList.cs:16](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Shopping/ShoppingList.cs#L16), [ShoppingListCalculator.cs:96](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Shopping/ShoppingListCalculator.cs#L96), [ShoppingListConfiguration.cs:12](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Infrastructure/Persistence/Configurations/ShoppingListConfiguration.cs#L12) | **POZYTYWNY (Rozszerzenie)**: Kod przewyższa wymagania PRD, dostarczając pełną spójność list wielodniowych i obsługę modyfikacji planu podczas zakupów. |
| **Bezpośrednie Produkty w Planie Posiłków**: PRD ([prd.md:137](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L137), FR-012) definiowało dodawanie wyłącznie przepisów do planu dnia. | Kod domeny umożliwia dodawanie surowych produktów bezpośrednio do planu posiłków (`MealPlanEntrySource.Product`) z zamrożeniem migawki odżywczej (`PlannedProductSnapshot`). | [MealPlanEntry.cs:47](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Planning/MealPlanEntry.cs#L47), [PlannedProductSnapshot.cs:13](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Planning/PlannedProductSnapshot.cs#L13) | **POZYTYWNY (Rozszerzenie)**: Znacznie poprawia UX (użytkownik nie musi tworzyć 1-składnikowego przepisu na banan czy jabłko). |
| **Tagi Przepisu (`Tag` / `RecipeTag`)**: Pierwotna koncepcja ([jadlify-koncepcja-mvp.md:104](file:///c:/Users/wikla/source/repos/Jadlify/docs/jadlify-koncepcja-mvp.md#L104)) wymieniała tagi przepisów (`wege`, `meal prep`) jako funkcję obowiązkową MVP. | PRD ([prd.md:191](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L191)) przesunęło tagi do Non-Goals. W kodzie domenowym brak jakichkolwiek klas tagów. | Brak plików `Tag.cs` / `RecipeTag.cs` w `src/Jadlify.Domain/Recipes/` | **BRAK**: Zgodne z oficjalnym zakresem PRD (wycięte ze scope MVP). |

---

## KROK 5 — Ranking Refaktoryzacji (Refactoring Priority Matrix)

Kandydaci na refaktoryzację zostali uszeregowani według stosunku **Wartości Biznesowej** (znaczenie dla niezmienników Core Subdomain) do **Ryzyka** (aktualny poziom braku egzekwowania lub wycieku logiki).

```mermaid
quadrantChart
    title Priorytetyzacja Refaktoryzacji Domeny
    x-axis Niski Wyciek/Ryzyko --> Wysoki Wyciek/Ryzyko
    y-axis Niska Wartość Core --> Wysoka Wartość Core
    quadrant-1 PILNY REFAKTOR (#1)
    quadrant-2 PLANOWANY REFAKTOR (#2)
    quadrant-3 NISKI PRIORYTET
    quadrant-4 MONITORUJ / DOKUMENTUJ
    "Wprowadzenie Agregatu MealPlanDay": [0.85, 0.90]
    "Obsługa Sierot po Usunięciu Przepisu": [0.75, 0.80]
    "Enkapsulacja Unikalności Aktywnej Listy Zakupów": [0.35, 0.70]
    "Usunięcie Produktu a Migawki Składników": [0.20, 0.40]
```

### #1 Kandydat do Refaktoryzacji: Wprowadzenie Agregatu `MealPlanDay` (Plan Dnia)
- **Wartość Biznesowa**: **Bardzo Wysoka** (Core Subdomain). Dzień jest główną jednostką bilansowania diety i źródłem dla listy zakupów.
- **Aktualne Ryzyko**: **Wysokie**. Logika spójności dnia jest rozproszona w serwisach aplikacji (`GetDailyMacroSummaryQueryHandler`, `MealPlanDayProjection`). Brak granicy transakcyjnej powoduje, że usunięty przepis cicho obniża sumę kalorii dnia bez śladu.
- **Proponowane Działanie Refaktoryzacyjne**:
  1. Stworzyć korzeń agregatu `MealPlanDay` zarządzonego przez `UserId` i `DateOnly`.
  2. Przenieść kolekcję `MealPlanEntry` doewnątrz `MealPlanDay`.
  3. Zapewnić walidację referencji przepisów i zgłaszanie ostrzeżeń w przypadku usunięcia przepisu ze źródła.

### #2 Kandydat do Refaktoryzacji: Obsługa Sierot i Wersjonowanie Przepisów w Planie
- **Wartość Biznesowa**: **Wysoka** (Core Subdomain).
- **Aktualne Ryzyko**: **Średnie/Wysokie**. Usunięcie przepisu używanego w planie dnia niszczy podsumowanie makro bez powiadomienia.
- **Proponowane Działanie Refaktoryzacyjne**: Zastosowanie wzorca `RecipeSnapshot` we wpisach planu (analogicznie do istniejącego `PlannedProductSnapshot`) LUB wprowadzenie reguły bloku usunięcia używanego przepisu.

### #3 Kandydat do Refaktoryzacji: Enkapsulacja Niezmiennika Aktywnej Listy Zakupów
- **Wartość Biznesowa**: **Średnia** (Core Subdomain).
- **Aktualne Ryzyko**: **Niskie**. Reguła "tylko jedna aktywna lista zakupów per użytkownik" jest egzekwowana w handlerze komendy, ale nie na poziomie repozytorium/domeny.
- **Proponowane Działanie Refaktoryzacyjne**: Przeniesienie weryfikacji unikalności do serwisu domenowego `ShoppingListDomainService`.

---

## Podsumowanie (Summary)

Wytworzony artefakt zawiera pełną destylację domeny biznesowej systemu **Jadlify** zrealizowaną według metodologii Domain-Driven Design w oparciu o dokumentację projektową oraz kod C#/.NET 10. Dokument precyzyjnie definiuje Słownik Domenowy (Ubiquitous Language) wraz z cytatami ze źródeł oraz weryfikacją w kodzie, dokonuje klasyfikacji subdomen na Core, Supporting oraz Generic, wskazuje kluczowe niezmienniki domenowe dla agregatów `Recipe`, `ShoppingList`, `MealPlanEntry` i `Product`, a także sporządza rejestr rozjazdów pomiędzy modelem dokumentacyjnym a realnym kodem. Najważniejszym wnioskiem z analizy jest zidentyfikowanie braku wyraźnego korzenia agregatu dla dnia planu posiłków (`MealPlanDay`) — kod traktuje wpisy planu jako płaskie encje `MealPlanEntry`, co powoduje wyciek logiki bilansowania dnia do warstwy aplikacji i potencjalne niespójności odżywcze po usunięciu przepisów. Dokument rekomenduje ten obszar jako bezwzględny priorytet #1 do przyszłej refaktoryzacji.
