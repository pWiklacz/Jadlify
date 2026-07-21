# UI Redesign and Expanded Planning Implementation Plan

## Overview

Przebudowujemy całą powierzchnię Jadlify zgodnie z językiem wizualnym mockupów z `docs/design/`: ciepły ciemny shell, kremowe karty, fonty Archivo i Instrument Serif, polskie copy, spójne stany ładowania/błędu/pustki oraz pełna responsywność desktop/mobile. Zmiana obejmuje również uzgodnione rozszerzenia: wyszukiwanie, sortowanie i szczegóły produktów/przepisów, pełny planer dzień/tydzień/miesiąc z wpisami przepisowymi i produktowymi, trwałe listy zakupów z historią oraz potwierdzanym diffem, a także interaktywny dashboard dnia.

Plan zachowuje istniejące granice architektury: przeglądarka używa Supabase wyłącznie dla sesji, dane domenowe przechodzą przez ASP.NET Core API, wszystkie zasoby pozostają owner-scoped, a kalkulacje makro korzystają z jednego deterministycznego rdzenia domenowego. Mockupy są źródłem wyglądu i zachowań UX; istniejące kontrakty i jawne decyzje tego planu są źródłem semantyki danych.

## Current State Analysis

Frontend React ma stabilny router i feature-foldery, ale nie ma design tokenów ani współdzielonej biblioteki UI. `tailwind.config.js` jest stockowy, `index.css` zawiera wyłącznie dyrektywy Tailwind, a obecny shell i ekrany używają powtarzanych klas slate-on-white. Strona główna nadal jest technicznym placeholderem opartym na `/api/me`.

Produkty obsługują CRUD, lookup kodu, rozmiar opakowania i rozszerzone wartości żywieniowe. Marka jest dostępna tylko jako sugestia z lookupu i nie jest zapisywana; kategoria nie istnieje. Przepisy mają pełne dane szczegółowe i live preview, ale lista ładuje ciężki model wraz ze składnikami. Cele są jednym aktualnym singletonem GET/PUT.

Plan posiłków jest dziś recipe-only, jednodniowy i używa całkowitych porcji. Data i źródło wpisu są celowo niezmienne; edycja zmienia jedynie typ posiłku i porcje. Lista zakupów jest jednodniową projekcją compute-on-read bez własnej encji, historii ani stanu „kupione”.

### Key Discoveries

- Router i chroniony shell są scentralizowane w `src/Jadlify.Web/src/App.tsx:12` i `src/Jadlify.Web/src/layout/AppShell.tsx:16`; redesign nie wymaga zmiany hostingu SPA ani granicy auth.
- Warstwa danych frontendu jest oddzielona od JSX przez hooki TanStack Query (`context/changes/ui-redesign/research.md:153`).
- `packageSizeGrams` oraz 14 pól żywieniowych już istnieją w `src/Jadlify.Web/src/products/types.ts:14`; nowe pola produktu to opcjonalna marka i opcjonalna kategoria.
- Marka jest zwracana przez barcode lookup, ale nie przechodzi do `Product`/Create/Update (`src/Jadlify.API/Products/ProductContracts.cs:139`).
- Planer ma obowiązkowy `RecipeId`, `int Portions` oraz tylko `ListByDateAsync` (`src/Jadlify.Domain/Planning/MealPlanEntry.cs:3`, `src/Jadlify.Application/Planning/IMealPlanRepository.cs:9`).
- Dzienne makro i shopping opierają się na parach wpis+przepis; wspólne projekcje trzeba rozszerzyć o bezpośredni produkt (`src/Jadlify.Domain/Nutrition/MacroCalculator.cs:40`, `src/Jadlify.Domain/Shopping/ShoppingListCalculator.cs:7`).
- Obecna lista zakupów nie ma persystencji ani agregatu (`src/Jadlify.Application/Shopping/GetShoppingList/GetShoppingListQueryHandler.cs:10`, `src/Jadlify.Infrastructure/Persistence/JadlifyDbContext.cs:17`).
- Mockup planera potrzebuje maksymalnie 42 dni do siatki miesiąca; jeden bounded range query pozwala uniknąć 42 requestów i N+1.
- Testy UI lokalizują elementy po rolach i dostępnych nazwach. Polonizacja jest migracją kontraktów semantycznych testów (`context/changes/ui-redesign/research.md:129`).
- `context/foundation/test-plan.md` wyklucza snapshoty statycznego DOM/layoutu; wizualny odbiór łączy semantyczne testy z manualnym porównaniem desktop/940/mobile.

## Desired End State

Zalogowany użytkownik korzysta z jednego spójnego, polskiego i responsywnego interfejsu. Może wyszukiwać, filtrować i przeglądać szczegóły produktów oraz przepisów, planować posiłki w widoku dnia, tygodnia i miesiąca, używać przepisów w krokach po 0,5 porcji albo pojedynczych produktów w gramach, a także atomowo przenosić i kopiować wpisy lub całe dni.

Użytkownik może utworzyć trwałą listę z arbitralnie wybranych dni, odhaczać produkty, przełączać widok wg produktów/posiłków/dni, kończyć listę i wracać do historii. Gdy plan źródłowy się zmieni, aplikacja pokazuje trzysekcyjny diff i stosuje go dopiero po potwierdzeniu. Dashboard wybranego dnia pokazuje bilans, posiłki, stan zakupów i kontekstowy następny krok oraz korzysta z tych samych akcji co planer.

Stan końcowy jest potwierdzony przez testy domenowe, aplikacyjne, persistence, API, RTL i E2E, a także manualny smoke wszystkich tras w szerokościach desktop, 940 px oraz wąskiego mobile. Migracje przechodzą na bazie zawierającej istniejące produkty, przepisy i wpisy planu.

## What We're NOT Doing

- Nie wdrażamy resetu/zmiany hasła, potwierdzania e-mail, ustawień konta ani usunięcia konta.
- Nie dodajemy usuwania celu, `updatedAt`, historii celów ani profili celów.
- Nie dodajemy skanowania kodu aparatem, eksportu listy, trybu offline, aplikacji natywnej, AI ani funkcji społecznościowych.
- Nie dodajemy drag-and-drop. Move/copy pozostają dostępnymi z klawiatury akcjami.
- Nie zapisujemy stanu list ani handoffów w `localStorage`; trwałe dane przechodzą przez owner-scoped API.
- Nie zmieniamy blokady usuwania przepisu użytego w planie. Recipe entries nadal odwołują się do bieżącego przepisu; bezpośredni product entry zapisuje snapshot produktu.
- Nie budujemy globalnej pięciostopniowej checklisty onboardingu. Dashboard pokazuje jeden kontekstowy „Następny krok”.
- Nie deklarujemy formalnej certyfikacji WCAG-AA i nie dodajemy snapshotów DOM/layoutu.
- Nie obsługujemy range większego niż 42 dni ani nieograniczonych operacji batch.

## Implementation Approach

Implementacja idzie od stabilnych kontraktów do powierzchni UI. Najpierw powstaje design system i dostępny shell, następnie małe rozszerzenie produktu i katalog, potem read modele przepisów. Dopiero po tych fundamentach zmieniamy planner domain/persistence/API, dodajemy atomowe operacje i migrujemy UI planera. Trwałe listy powstają na gotowej projekcji planu, a dashboard zamyka integrację jako ostatni konsument współdzielonych hooków i komponentów.

Każda faza zachowuje działającą aplikację i kończy się automatycznym oraz manualnym gate. Backendowe operacje batch zapisują dane raz, w transakcji i po owner-scoped walidacji całego żądania. Frontendowe query keys obejmują filtry i zakresy, a mutacje invalidują wspólne prefiksy zamiast ręcznie synchronizować kilka kopii stanu.

## Critical Implementation Details

### Planner source invariants

`MealPlanEntry` ma dokładnie jeden wariant źródła. Recipe entry przechowuje `RecipeId` i dodatnią liczbę porcji będącą wielokrotnością `0.5`; product entry przechowuje dodatnie gramy i owned snapshot `ProductId`, nazwy, kategorii oraz makro/100 g. Wire request ma jawne pola wariantu (`recipeId`+`portions` albo `productId`+`grams`), aby jednostka nigdy nie była domyślana z jednej ogólnej wartości.

Istniejące recipe entries po migracji zachowują bieżące zachowanie: edycja przepisu wpływa na przyszłe obliczenia planu, a usunięcie użytego przepisu nadal zwraca konflikt. Product snapshot przejmuje politykę recipe ingredients: późniejsza edycja/usunięcie katalogowego produktu nie zmienia zaplanowanego wpisu.

### Shopping-list synchronization

Aktywna lista zapisuje wybrane dni, snapshot pozycji oraz wkłady źródłowe potrzebne do widoków wg posiłków i dni. `SourceFingerprint` jest liczony z kanonicznie posortowanych wkładów planu, nie tylko z końcowych sum. Preview diff nie zapisuje danych. Refresh przyjmuje oczekiwane wersje listy i źródła, ponownie liczy projekcję w transakcji i zwraca `409` przy rozjeździe.

Po zatwierdzeniu diffu niezmieniona pozycja po `ProductId` zachowuje `IsBought`; pozycja dodana albo ze zmienioną gramaturą jest odznaczona; usunięta znika. Zmiana wyłącznie źródeł zachowuje `IsBought`, ale nadal wymaga potwierdzenia. Ukończone listy są zamrożonym snapshotem.

### Responsive and accessible rendering

Breakpoint 940 px trafia do Tailwind/CSS. Nie tworzymy dwóch drzew UI sterowanych `window.innerWidth`; warunkowe montowanie jest używane wyłącznie tam, gdzie zmienia się stan lub semantyka. Dialog ma jeden kontrakt: dostępna nazwa, initial focus, pełny focus trap, Escape, backdrop policy, body scroll lock i zwrot fokusu. Mobile używa tego samego dialogu jako bottom sheet.

## Phase 1: Fundament design systemu, shell i auth

### Overview

Tworzymy kanoniczne tokeny, prymitywy i dostępny shell, a następnie migrujemy auth surface.

### Changes Required

#### 1. Tokeny, fonty i dokument

**Files**: `src/Jadlify.Web/tailwind.config.js`, `src/Jadlify.Web/src/index.css`, `src/Jadlify.Web/index.html`

**Intent**: Zakodować paletę, fonty, radiusy, cienie, animacje i breakpoint `design: 940px`; ustawić `lang="pl"`, tabular nums, focus i bezpieczne zawijanie.

**Contract**: Nazwane tokeny są jedynym źródłem wartości designu; komponenty nie kopiują surowych hexów z mockupów. Fonty mają systemowe fallbacki.

#### 2. Wspólne prymitywy UI

**Files**: nowe `src/Jadlify.Web/src/ui/{Button,Card,PageHeader,Dialog,Toast,QueryState,Field,Stepper,MacroCompareRow,StatusPill}.tsx`, `formatters.ts` i testy

**Intent**: Zastąpić powtarzane klasy i rozbieżne zachowania komponentami współdzielonymi, w tym decimal parsing z przecinkiem, polską odmianę i mapowanie wire meal type.

**Contract**: `Dialog` obsługuje `dialog`/`alertdialog` i focus lifecycle. `Toast` używa `status` dla pending/success/info i `alert` dla błędu. `Stepper` przyjmuje jawne `step`, `min`, `max` i accessible label.

#### 3. Shell, konto i logowanie

**Files**: `src/Jadlify.Web/src/layout/{AppShell,AccountMenu,navItems}.*`, `src/Jadlify.Web/src/routes/LoginPage.tsx`, testy layout/auth

**Intent**: Wdrożyć logo, status pill, sześć polskich pigułek nav, przewijany nav mobile, konto/wylogowanie i docelowy auth layout.

**Contract**: Trasy i auth pozostają bez zmian. Aktywna trasa ma `aria-current="page"`; menu konta implementuje pełny keyboard pattern albo pozostaje popoverem bez niepełnego `role=menu`.

### Success Criteria

#### Automated Verification

- Testy UI/layout/auth: `cd src/Jadlify.Web; npm test -- src/ui src/layout src/routes src/App.test.tsx`.
- Lint: `cd src/Jadlify.Web; npm run lint`.
- Build: `cd src/Jadlify.Web; npm run build`.

#### Manual Verification

- Shell/nav/auth odpowiadają mockupom na 1440, 940 i 390 px; brak overflow i touch targets poniżej 44 px.
- Klawiatura obsługuje nav, konto i dialog; focus jest widoczny i wraca do triggera.

**Implementation Note**: Zatrzymać fazę do manualnego potwierdzenia shellu i focus lifecycle.

---

## Phase 2: Metadane produktu i kontrakty katalogu

### Overview

Rozszerzamy produkt o opcjonalną markę i kategorię oraz dodajemy lekki, paginowany kontrakt katalogu.

### Changes Required

#### 1. Domena i taksonomia

**Files**: `src/Jadlify.Domain/Products/Product.cs`, nowy `ProductCategory.cs`, testy Domain

**Intent**: Dodać edytowalną markę i kategorię, pozostawiając „Bez kategorii” jako poprawny stan. Kanon: warzywa, owoce, mięso i ryby, nabiał, zbożowe i pieczywo, sypkie i spiżarnia, mrożonki, napoje, inne.

**Contract**: Wire enum używa stabilnych nazw angielskich, UI mapuje polskie etykiety. `Brand` i `Category` są nullable; `PackageSizeGrams` nie jest duplikowane.

#### 2. Application/API i OFF

**Files**: `src/Jadlify.Application/Products/**`, `src/Jadlify.API/Products/{ProductContracts,ProductEndpoints}.cs`, adapter OFF i testy

**Intent**: Propagować brand/category przez CRUD i mapować wspierane kategorie OFF jako opcjonalną sugestię. Dodać paginowany catalog query.

**Contract**: Istniejący `GET /api/products` pozostaje kompatybilny. Nowy `GET /api/products/catalog` zwraca `items,total,skip,take` i obsługuje search/category/sort. Nieznana kategoria OFF daje `null`, nigdy błąd.

#### 3. Persistence i migracja

**Files**: `src/Jadlify.Infrastructure/Persistence/Configurations/ProductConfiguration.cs`, `Repositories/ProductRepository.cs`, nowa migracja i model snapshot

**Intent**: Dodać nullable `brand`/`category`, indeks owner+category+name i owner-scoped catalog query.

**Contract**: Istniejące rekordy dostają null; migracja nie zmienia makro ani package size. Cross-user zachowanie pozostaje NotFound/brak wyników.

### Success Criteria

#### Automated Verification

- Testy: `pwsh ./.scripts/test-min.ps1 -FullyQualifiedNameContains Product`.
- Build: `pwsh ./.scripts/build-min.ps1 -Project ./Jadlify.slnx`.
- Migracja i create/update/get/catalog obejmują null, wartości oraz cross-user isolation.

#### Manual Verification

- Migracja stosuje się na bazie z istniejącym produktem bez utraty danych.
- Lookup z marką pozwala ją zaakceptować, zmienić lub wyczyścić; brak kategorii nie blokuje zapisu.

**Implementation Note**: Potwierdzić migrację na kopii danych przed UI katalogu.

---

## Phase 3: Nowy katalog produktów

### Overview

Budujemy index, filtry, sortowanie, szczegóły i formularze produktu.

### Changes Required

#### 1. Router i hooki

**Files**: `src/Jadlify.Web/src/App.tsx`, `src/Jadlify.Web/src/products/{useProducts,useProduct,useProductCatalog,types,useProductMutations}.*`

**Intent**: Dodać `/products/:id`, paginowany query i detail. Query keys zawierają search/category/sort/page; mutacje invalidują catalog i detail.

**Contract**: URL jest źródłem id i filtrów. Picker przepisu nadal używa lekkiego search endpointu.

#### 2. Index i karty

**Files**: `src/Jadlify.Web/src/products/ProductsPage.tsx`, nowe `ProductCard.tsx`, `ProductFilters.tsx`, testy

**Intent**: Wdrożyć search/category/sort, chipy, licznik PL, grid, skeleton, retry error, empty i no-results.

**Contract**: Search jest debounced, page resetuje się po filtrze, a długie nazwy zawijają się zamiast znikać.

#### 3. Detail i formularz

**Files**: nowe `ProductDetailsPage.tsx`, przebudowane `ProductFormModal.tsx`, `DeleteProductDialog.tsx`, testy

**Intent**: Odtworzyć dane na 100 g/całe opakowanie, wartości rozszerzone, brand/category/package, lookup states i guard niezapisanych zmian.

**Contract**: Detail używa istniejącego GET id. Barcode NotFound/partial prowadzi do ręcznego zapisu. Desktop używa dialogu, mobile bottom-sheetu ze sticky save.

### Success Criteria

#### Automated Verification

- Testy: `cd src/Jadlify.Web; npm test -- src/products`.
- Lint/build: `cd src/Jadlify.Web; npm run lint`; `cd src/Jadlify.Web; npm run build`.
- Pokryte są filtry, detail, lookup, guard i invalidacje cache.

#### Manual Verification

- Index/detail/CRUD/barcode fallback odpowiadają mockupowi na trzech szerokościach.
- Powrót zachowuje filtry; focus, scroll-lock, długie nazwy i 14 pól nie łamią layoutu.

**Implementation Note**: Odbiór obejmuje create→detail→edit→delete na owner-scoped danych.

---

## Phase 4: Przepisy i dzienne cele

### Overview

Migrujemy przepisy do lekkiego katalogu ze szczegółami i akcją „Dodaj do planu”, a cele do stanów view/edit/empty z ostrzeżeniem spójności, bez rozszerzania goal API.

### Changes Required

#### 1. Recipe catalog read model

**Files**: `src/Jadlify.Application/Recipes/**`, `src/Jadlify.Infrastructure/Persistence/Repositories/RecipeRepository.cs`, `src/Jadlify.API/Recipes/{RecipeContracts,RecipeEndpoints}.cs`, testy

**Intent**: Dodać paginowany summary catalog z search/sort/total i `isInPlan`, aby index nie ładował wszystkich ingredient snapshots. Zachować pełny GET detail.

**Contract**: `GET /api/recipes/catalog` zwraca id, name, portions, ingredientCount, total/perServing macros i `isInPlan`. Odczyt usage jest owner-scoped i batchowy.

#### 2. Recipes UI i handoff

**Files**: `src/Jadlify.Web/src/App.tsx`, `src/Jadlify.Web/src/recipes/**`, nowe `RecipeDetailsPage.tsx`, catalog hooks i testy

**Intent**: Wdrożyć search/sort, badge „W PLANIE”, detail, reorder składników, sticky live summary i „Dodaj do planu”.

**Contract**: Handoff używa `/meal-plan?addRecipe=<id>&date=<yyyy-MM-dd>&returnTo=<encoded-path>`, nie localStorage. Planer waliduje owner-scoped id i usuwa parametry po przejęciu. Builder zachowuje whole-recipe grams i backend-authoritative math.

#### 3. Dzienne cele

**Files**: `src/Jadlify.Web/src/planning/DailyGoalsPage.tsx`, `useDailyGoal.ts`, macro components i testy

**Intent**: Dodać summary/empty/edit, nieblokujące ostrzeżenie `4P+4C+9F`, live preview względem planu dnia, błędy per-field, focus pierwszego błędu i unsaved guard.

**Contract**: Goal pozostaje singletonem GET/PUT bez id, historii, timestampu i delete. Ostrzeżenie nie blokuje zapisu. Delta FR-013 pozostaje tekstowa, nie tylko kolor/pasek.

### Success Criteria

#### Automated Verification

- Backend: `pwsh ./.scripts/test-min.ps1 -FullyQualifiedNameContains Recipe`.
- Frontend: `cd src/Jadlify.Web; npm test -- src/recipes src/planning/DailyGoalsPage.test.tsx`.
- Frontend lint/build i backend build przechodzą.

#### Manual Verification

- Recipe index/detail/builder/delete i picker działają na desktop/940/mobile bez N+1.
- Add-to-plan zachowuje recipe/date/return context.
- Goal empty/view/edit, warning i unsaved guard działają klawiaturą.

**Implementation Note**: Docelowy add-meal modal zostanie podmieniony w Fazie 7 bez zmiany URL contract.

---

## Phase 5: Rdzeń pełnego planera

### Overview

Rozszerzamy domenę, persistence i read API o fractional recipe portions, bezpośrednie produkty oraz bounded range z podsumowaniami per dzień.

### Changes Required

#### 1. Discriminated meal-plan domain

**Files**: `src/Jadlify.Domain/Planning/MealPlanEntry.cs`, nowe source/snapshot value objects, `MacroCalculator.cs`, `ShoppingListCalculator.cs`, testy Domain

**Intent**: Zastąpić recipe-only/int-only modelem dokładnie jednego wariantu źródła. Rozszerzyć wspólną kalkulację o product snapshot przy pełnej precyzji decimal.

**Contract**: Recipe portions są dodatnią wielokrotnością `0.5`. Product grams są dodatnim decimal; UI krok 10 g nie jest ukrytą regułą domenową. Recipe entry zachowuje FK Restrict; product entry przechowuje snapshot id/name/category/per100 bez FK.

#### 2. Persistence i migracja

**Files**: `src/Jadlify.Infrastructure/Persistence/Configurations/MealPlanEntryConfiguration.cs`, `JadlifyDbContext.cs`, nowa migracja planner source/quantity, snapshot i testy

**Intent**: Dodać discriminator, decimal quantity i product snapshot; zbackfillować istniejące wpisy jako recipe source z dotychczasową liczbą porcji. Zachować indeks `(user_id,date)`.

**Contract**: Check constraint wymusza jeden wariant i dodatnią quantity. Migracja nie zmienia date/meal type/recipe id. Makro istniejących dni pozostaje identyczne z oracle.

#### 3. Range application/API

**Files**: `src/Jadlify.Application/Planning/**`, `src/Jadlify.API/Planning/{MealPlanContracts,MealPlanEndpoints}.cs`, frontend types i testy

**Intent**: Dodać create/update dla obu źródeł oraz `GET /api/meal-plan/range?from=&to=` z wpisami i summary pogrupowanymi per dzień.

**Contract**: Zakres inclusive, `from <= to`, maks. 42 dni. Handler pobiera entries raz, distinct recipes with ingredients raz i goal raz. Day zawiera total, nullable goal i signed remaining. Existing day routes pozostają kompatybilne w migracji.

### Success Criteria

#### Automated Verification

- Domain oracle dowodzi 0,5 porcji, product grams, mixed total i shopping aggregation.
- Testy: `pwsh ./.scripts/test-min.ps1 -FullyQualifiedNameContains MealPlan`.
- Migracja/repository/API pokrywają backfill, exactly-one-source, max 42 dni, brak N+1 i cross-user.
- Build: `pwsh ./.scripts/build-min.ps1 -Project ./Jadlify.slnx`.

#### Manual Verification

- Migracja zachowuje istniejący dzień i makro 1:1.
- API tworzy/edytuje recipe 0,5 i product grams, odrzuca ambiwalentne/puste źródło i cudzy id.
- Range dnia/tygodnia/42 dni ma stabilną kolejność bez request/query per dzień.

**Implementation Note**: Nie przechodzić do batch operations, dopóki migracja i oracle istniejących wpisów nie są potwierdzone.

---

## Phase 6: Atomowe operacje planera

### Overview

Dodajemy owner-scoped move/copy/copy-day z pełną walidacją przed zapisem i jednym transakcyjnym commit.

### Changes Required

#### 1. Domain/application commands

**Files**: `src/Jadlify.Domain/Planning/MealPlanEntry.cs`, nowe `Application/Planning/MealPlans/{Move,Copy,CopyDay}*`, validators i testy

**Intent**: Umożliwić zmianę daty przez nazwane zachowanie i kopie z nowymi id. Zdefiniować copy-day `Add`/`Replace` i zamknięte limity targetów/wpisów.

**Contract**: Move zachowuje id/source/quantity i opcjonalnie zmienia meal type. Copy klonuje recipe reference albo product snapshot. Copy-day waliduje wszystko przed mutacją; Replace usuwa wyłącznie owner-scoped target entries w tej samej transakcji.

#### 2. Atomowa persistence

**Files**: `src/Jadlify.Application/Planning/IMealPlanRepository.cs`, `src/Jadlify.Infrastructure/Persistence/Repositories/MealPlanRepository.cs`, testy

**Intent**: Dodać bounded range reads i dedykowane atomowe metody/store dla batch zamiast wielu `SaveChanges`.

**Contract**: Zero częściowych zapisów. Cudzy/missing source lub jeden niepoprawny target powoduje pełny rollback.

#### 3. API operations

**Files**: `src/Jadlify.API/Planning/{MealPlanContracts,MealPlanEndpoints}.cs`, API tests

**Intent**: Udostępnić move, copies i copy-day jako semantyczne endpointy zgodne z Result/ProblemDetails.

**Contract**: `POST /api/meal-plan/{id}/move`, `POST /api/meal-plan/{id}/copies`, `POST /api/meal-plan/days/{date}/copies`; response identyfikuje nowe wpisy i daty. Cross-user 404, validation 400, conflict 409.

### Success Criteria

#### Automated Verification

- Move/copy/copy-day pokrywają recipe/product, Add/Replace, limity, rollback i cross-user.
- Testy: `pwsh ./.scripts/test-min.ps1 -FullyQualifiedNameContains MealPlan`.
- Infrastructure potwierdza jeden transaction/commit i brak partial replace.

#### Manual Verification

- Move/copy do wielu dni oraz Add/Replace zwracają oczekiwane range results.
- Celowo błędny batch nie zmienia żadnego dnia.

**Implementation Note**: Manualny gate obejmuje obserwację rollbacku na błędnym elemencie batch.

---

## Phase 7: UI planera dzień/tydzień/miesiąc

### Overview

Wdrażamy pełną powierzchnię planera na range API i batch operations, utrzymując jeden cache/query model.

### Changes Required

#### 1. Range hooks, URL state i cache

**Files**: `src/Jadlify.Web/src/planning/{useMealPlan,useDailyMacroSummary,useMealPlanMutations,types}.*`, nowe range/operation hooks

**Intent**: Zastąpić dzienne odczyty jednym range query i scentralizować invalidację. Utrwalić view/date w URL.

**Contract**: `/meal-plan?view=day|week|month&date=yyyy-MM-dd`. Zmiana zakresu generuje jeden range request. Mutacje invalidują prefiks `meal-plan` i shopping source-change state.

#### 2. Day/week/month rendering

**Files**: przebudowany `MealPlanPage.tsx`, nowe `PlannerRangeNav.tsx`, `DayCard.tsx`, `MealGroup.tsx`, `MealEntryCard.tsx`, `MonthGrid.tsx`, macro components i testy

**Intent**: Odtworzyć 7 kart tygodnia, panel dnia, summary tygodnia i 42-komórkowy miesiąc wraz ze stanami loading/error/empty/background-refresh.

**Contract**: Każdy wpis pokazuje tekstową i liczbową informację makro. Status celu używa jednego progu ±5%. Wszystkie widoki korzystają z tych samych DTO; month nie uruchamia requestów per komórka.

#### 3. Interakcje i handoffy

**Files**: add/replace/move/copy/copy-day dialogs, `MealPlanEntryForm.tsx`, pickers, Toast i testy

**Intent**: Obsłużyć Recipe/Product, stepper 0,5/10 g, move/copy, copy-day Add/Replace, delete+undo, FAB mobile i handoff z detail.

**Contract**: Dialog waliduje source i jednostkę. Undo odtwarza wpis wspólną create mutation i raportuje konflikt. Bez localStorage; wszystkie akcje mają keyboard alternative.

### Success Criteria

#### Automated Verification

- Testy: `cd src/Jadlify.Web; npm test -- src/planning`.
- Pokryte są URL state, day/week/month, one-range-request, operacje, undo i cache invalidation.
- Lint/build przechodzą.

#### Manual Verification

- Day/week/month odpowiadają mockupowi na 1440/940/390; miesiąc ma 42 poprawne komórki.
- Network pokazuje jeden range request i brak refetch storm.
- Dialogi/FAB/steppery działają klawiaturą, a content nie overflowuje.

**Implementation Note**: Przejść recipe 0,5, product grams, move, copy, Replace i undo przed Fazą 8.

---

## Phase 8: Trwałe listy zakupów

### Overview

Tworzymy osobny agregat listy, snapshoty źródeł, historię, concurrency i czysty kalkulator diffu, zachowując obecny calculator jako jądro agregacji.

### Changes Required

#### 1. Domain aggregate i diff

**Files**: nowe `src/Jadlify.Domain/Shopping/{ShoppingList,ShoppingListItem,ShoppingListSourceDay}.*`, contribution/diff records, rozszerzony `ShoppingListCalculator.cs`, testy

**Intent**: Modelować Active/Completed, wybrane dni, item/source snapshots, toggle bought, complete oraz preview/apply diff.

**Contract**: Maks. jedna aktywna lista per user. Item grupowany po `ProductId` przechowuje name/category/grams i contributions. Completed jest immutable. Diff ma Added/Removed/Changed/SourceOnly; Changed/Added resetują bought, Unchanged/SourceOnly zachowują.

#### 2. Persistence, concurrency i transakcje

**Files**: `JadlifyDbContext.cs`, nowe EF configurations/repository, migracja shopping lists, snapshot i testy

**Intent**: Dodać `shopping_lists`, `shopping_list_source_days`, `shopping_list_items`, `shopping_list_item_sources`, owner indexes, unique active constraint i version.

**Contract**: Dzieci są dostępne przez owner-scoped root. Item/source nie mają FK do usuwalnego produktu/przepisu. Create/refresh/complete używają jednego SaveChanges w transakcji; refresh odrzuca stale expected versions.

#### 3. Application/API

**Files**: nowe `src/Jadlify.Application/Shopping/ShoppingLists/**`, `IShoppingListRepository.cs`, `src/Jadlify.API/Shopping/{ShoppingListContracts,ShoppingListEndpoints}.cs`, testy

**Intent**: Udostępnić index active+history, create z arbitralnymi dniami, detail, toggle, diff, refresh i complete. Zachować stary endpoint do migracji UI.

**Contract**: `GET/POST /api/shopping-lists`, `GET /{id}`, `PATCH /{id}/items/{itemId}`, `GET /{id}/diff`, `POST /{id}/refresh`, `POST /{id}/complete`. Dni są unikalne, bounded i mieszczą się w span 42 dni. Stale state daje 409 bez mutacji.

### Success Criteria

#### Automated Verification

- Domain pokrywa mixed aggregation, snapshoty, diff i bought rules.
- Testy: `pwsh ./.scripts/test-min.ps1 -FullyQualifiedNameContains ShoppingList`.
- Persistence/API pokrywa unique active, historię, transakcje, stale 409, cross-user 404 i rollback.
- Build przechodzi.

#### Manual Verification

- Lista przeżywa reload, zachowuje odhaczenia i po complete jest w historii.
- Zmiana planu tworzy prawidłowy diff; preview nie zapisuje, confirm stosuje atomowo.
- Stale refresh zwraca 409 i ponowne preview nie gubi postępu.

**Implementation Note**: Sprawdzić zmianę ilości kupionej pozycji — po refresh ma zostać odznaczona.

---

## Phase 9: Lista UI, dashboard i integracja końcowa

### Overview

Migrujemy shopping UI na trwałe kontrakty, budujemy dashboard na współdzielonych komponentach planera i kończymy migrację testów/copy.

### Changes Required

#### 1. Shopping index/create/detail

**Files**: `src/Jadlify.Web/src/shopping/**`, route wrapper, hooks/types/components i testy

**Intent**: Wdrożyć active/history, dwukrokowy kreator dni, widoki Zakupy/Wg posiłków/Wg dni, sticky progress, checkboxy, complete i wszystkie stany.

**Contract**: Query keys rozdzielają index/detail/diff. Toggle jest optymistyczny z rollbackiem i expected version. Source days pozostają arbitralnym bounded zbiorem.

#### 2. Diff modal

**Files**: nowe `ShoppingListDiffDialog.tsx`, refresh hooks, Toast/Banner i testy

**Intent**: Pokazać trzysekcyjny diff i wymagać potwierdzenia; obsłużyć stale 409 przez ponowny preview.

**Contract**: Added/Removed/Changed mają ilości przed/po. Confirm wysyła wersje z preview; Cancel nie mutuje. SourceOnly nie resetuje bought.

#### 3. Interaktywny dashboard

**Files**: `src/Jadlify.Web/src/routes/LandingPage.tsx`, nowe `dashboard/*`, współdzielone planning/shopping hooks i testy

**Intent**: Dodać date nav, bilans/ring, macro rows, grupy posiłków z add/edit/delete, aktywną listę i jedno „Następny krok”.

**Contract**: Dashboard używa range jednego dnia, active list summary i tych samych mutation hooks/components co planner. CTA: brak produktów → produkty; brak przepisów → przepisy; brak celu → cele; brak planu → dodaj posiłek; brak listy → utwórz listę; inaczej → planer/lista.

#### 4. Finalna migracja testów i E2E

**Files**: dotknięte `*.test.tsx`, `src/Jadlify.Web/tests/e2e/seed.spec.ts`, nowy planner-shopping spec, README frontendu

**Intent**: Zaktualizować accessible locators do polskiego copy, usunąć asercje klas kolorów, rozszerzyć krytyczny flow i udokumentować prereq.

**Contract**: Testy używają ról/labeli, bez `waitForTimeout` i snapshotów layoutu. E2E używa lokalnego auth/backendu i owner-scoped API.

### Success Criteria

#### Automated Verification

- Frontend: `cd src/Jadlify.Web; npm test`; `cd src/Jadlify.Web; npm run lint`; `cd src/Jadlify.Web; npm run build`.
- Backend: `pwsh ./.scripts/verify-min.ps1 -BuildProject ./Jadlify.slnx -TestProject ./Jadlify.slnx`.
- Przy lokalnym backendzie/Supabase przechodzą seed i nowy planner-shopping E2E.
- Pokryte są dashboard actions, shopping diff/stale, cache i polskie accessible names.

#### Manual Verification

- Smoke przechodzi login, dashboard, produkty, przepisy, planer, cele, listę i wylogowanie.
- Flow product→recipe→goal→week/month plan→persistent list→plan change→diff→history działa bez obejścia.
- Każdy ekran jest porównany z mockupem na 1440/940/390; sprawdzone focus, touch, overflow, loading/error/empty i kontrast.
- Network nie pokazuje N+1, 42 requestów ani refetch storm; feedback/progress spełnia NFR.

**Implementation Note**: Change kończy się dopiero po manualnym odbiorze pełnego flow.

---

## Testing Strategy

### Unit Tests

- UI primitives: dialog focus, toast semantics, stepper bounds, PL plural/format/parse i macro statuses.
- Product brand/category validation oraz OFF suggestion mapping.
- Meal-plan source invariant, 0,5 portions, product grams, mixed totals i copy semantics.
- Shopping aggregation, fingerprint, Added/Removed/Changed/SourceOnly oraz bought rules.
- Kalkulacje mają niezależny `// Oracle:` i nie zaokrąglają wartości pośrednich.

### Integration Tests

- EF migrations i owner-scoped repositories produktu, range planner i shopping aggregate.
- Atomowość move/copy/Replace oraz refresh; błąd jednego elementu daje pełny rollback.
- API 400/404/409, batch bounds, max 42 dni, expected versions i cross-user matrix.
- Product/recipe pagination, total, filters/sort i brak ciężkich loads na index.

### Frontend Component Tests

- Shell/nav/account/auth z polskimi accessible names i `aria-current`.
- Loading/error/retry/empty/no-results/success i long-content na wszystkich ekranach.
- URL state, add-to-plan handoff i cache invalidation.
- Dashboard partial errors, planner day/week/month, shopping active/history/diff/stale.

### End-to-End Tests

- Zmigrowany seed product flow po polskich rolach/labelach.
- Product z fallbackiem → recipe → goal → entry 0,5 → copy-day → persistent list → bought → plan change → diff → complete.
- E2E używa lokalnego Supabase/backendu; OFF pozostaje stubowany na granicy.

### Manual Testing Steps

1. Smoke w 1440 px, 940 px i 390 px.
2. Stany mockupów należące do zakresu: loading, error, empty, partial, success, stale, modal i mobile.
3. Klawiatura, focus return/trap, Escape, scroll lock i touch targets.
4. Długie nazwy, duże wartości i pełny formularz żywieniowy.
5. Network/query count dla katalogów, month range, dashboardu i diffu.
6. Fonty, spacing, kolory, tekstowe statusy i brak zależności od samego koloru.

## Performance Considerations

- Katalogi są paginowane; index używa summary DTO, full details wyłącznie detail endpoints.
- Range planner ma limit 42 dni, jeden read entries, jeden batch recipes with ingredients i jedno goal read; nie wywołuje daily handlera w pętli.
- Month/dashboard nie używają ciężkiego recipe endpointu z ingredients.
- Product search jest providerowo case-insensitive i paginowany; owner/category/name index wspiera filtry i sort przy skali PRD.
- Batch copy ma limity targetów i tworzonych entries, walidowane przed zapisem.
- Shopping diff liczy jedną projekcję dla unikalnych source days, nie query per dzień/item.
- Animacje respektują `prefers-reduced-motion`; skeletony/toasty nie blokują main thread.

## Migration Notes

1. Product migration dodaje nullable `brand`/`category` i zachowuje istniejące rekordy.
2. Planner migration dodaje discriminator/decimal/product snapshot, backfilluje recipe rows i dopiero włącza constraints; przed wdrożeniem kopia bazy i porównanie makro.
3. Shopping migration dodaje nowe tabele bez modyfikowania starej read-only projekcji. Stary endpoint zostaje do końca Fazy 9.
4. Każda migracja aktualizuje model snapshot i jest weryfikowana na pustej bazie oraz bazie z danymi.
5. Nie usuwać starych kolumn/endpointów, jeśli ich pozostawienie umożliwia rollback; destrukcyjny cleanup jest osobną zmianą.

## References

- `context/changes/ui-redesign/research.md`
- `context/foundation/prd.md`
- `context/foundation/tech-stack.md`
- `context/foundation/test-plan.md`
- `docs/reference/contract-surfaces.md`
- `docs/design/_plan-notes.md`
- `docs/design/Jadlify - Strona główna v5.dc.html`
- `docs/design/Jadlify - Plan posiłków.dc.html`
- `docs/design/Jadlify - Produkty.dc.html`
- `docs/design/Jadlify - Przepisy.dc.html`
- `docs/design/Jadlify - Dzienne cele.dc.html`
- `docs/design/Jadlify - Lista zakupów.dc.html`
- `docs/design/Jadlify - Logowanie i konto.dc.html`
- Existing planner: `src/Jadlify.Domain/Planning/MealPlanEntry.cs:3`, `src/Jadlify.API/Planning/MealPlanContracts.cs:10`
- Existing shopping: `src/Jadlify.Application/Shopping/GetShoppingList/GetShoppingListQueryHandler.cs:10`
- Existing product: `src/Jadlify.Domain/Products/Product.cs:5`, `src/Jadlify.API/Products/ProductContracts.cs:12`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles.

### Phase 1: Fundament design systemu, shell i auth

#### Automated

- [x] 1.1 Testy prymitywów, layoutu, tras i auth przechodzą — 3dca4f7
- [x] 1.2 Frontend lint przechodzi — 3dca4f7
- [x] 1.3 Produkcyjny frontend build przechodzi — 3dca4f7

#### Manual

- [x] 1.4 Shell i auth są zgodne wizualnie na desktop, 940 px i mobile — 3dca4f7
- [x] 1.5 Klawiatura, focus lifecycle i touch targets są poprawne — 3dca4f7

### Phase 2: Metadane produktu i kontrakty katalogu

#### Automated

- [x] 2.1 Testy produktu przechodzą we wszystkich warstwach — 05fc4a9
- [x] 2.2 Build rozwiązania przechodzi — 05fc4a9
- [x] 2.3 Migracja i owner-scoped catalog contracts są zweryfikowane — 05fc4a9

#### Manual

- [x] 2.4 Migracja zachowuje istniejące produkty — 05fc4a9
- [x] 2.5 Brand/category round-trip i barcode fallback działają — 05fc4a9

### Phase 3: Nowy katalog produktów

#### Automated

- [x] 3.1 Frontendowe testy produktów przechodzą — deef72a
- [x] 3.2 Frontend lint i build przechodzą — deef72a
- [x] 3.3 Filtry, detail, lookup i cache invalidation są pokryte — deef72a

#### Manual

- [x] 3.4 Product index/detail/CRUD odpowiada mockupom na trzech szerokościach — 79be743
- [x] 3.5 Focus, overflow i powrót z zachowaniem filtrów działają — 79be743

### Phase 4: Przepisy i dzienne cele

#### Automated

- [x] 4.1 Backend recipe catalog i usage testy przechodzą
- [x] 4.2 Frontend recipe i daily-goal testy przechodzą
- [x] 4.3 Frontend lint/build i backend build przechodzą

#### Manual

- [x] 4.4 Recipes index/detail/builder i Add-to-plan działają responsywnie
- [x] 4.5 Goal empty/view/edit, warning i unsaved guard działają dostępnie

### Phase 5: Rdzeń pełnego planera

#### Automated

- [ ] 5.1 Domain oracle pokrywa fractional recipe i product entries
- [ ] 5.2 Planning, migration, repository i API tests przechodzą
- [ ] 5.3 Build rozwiązania przechodzi

#### Manual

- [ ] 5.4 Migracja zachowuje istniejące wpisy i makro
- [ ] 5.5 Recipe 0.5, product grams i range 42 dni działają owner-scoped

### Phase 6: Atomowe operacje planera

#### Automated

- [ ] 6.1 Move/copy/copy-day testy przechodzą
- [ ] 6.2 Atomowość, limity, rollback i cross-user są potwierdzone

#### Manual

- [ ] 6.3 Move/copy i Add/Replace mają oczekiwane wyniki
- [ ] 6.4 Błędny batch nie pozostawia częściowych zmian

### Phase 7: UI planera dzień/tydzień/miesiąc

#### Automated

- [ ] 7.1 Frontendowe testy planera przechodzą
- [ ] 7.2 URL state, one-range-request, operacje i cache są pokryte
- [ ] 7.3 Frontend lint i build przechodzą

#### Manual

- [ ] 7.4 Day/week/month odpowiadają mockupowi na trzech szerokościach
- [ ] 7.5 Pełny recipe/product/move/copy/undo flow działa dostępnie

### Phase 8: Trwałe listy zakupów

#### Automated

- [ ] 8.1 Domain aggregation, diff i bought rules testy przechodzą
- [ ] 8.2 Persistence/API concurrency, ownership i rollback testy przechodzą
- [ ] 8.3 Build rozwiązania przechodzi

#### Manual

- [ ] 8.4 Active/history/toggle/complete przeżywają reload
- [ ] 8.5 Diff preview, refresh i stale conflict zachowują postęp poprawnie

### Phase 9: Lista UI, dashboard i integracja końcowa

#### Automated

- [ ] 9.1 Pełny frontend test/lint/build przechodzi
- [ ] 9.2 Pełny backend verify-min przechodzi
- [ ] 9.3 Krytyczne E2E product-planner-shopping przechodzi

#### Manual

- [ ] 9.4 Uwierzytelniony smoke wszystkich tras i pełnego flow przechodzi
- [ ] 9.5 Finalne porównanie desktop/940/mobile i accessibility QA przechodzi
- [ ] 9.6 Network/performance QA nie wykazuje N+1 ani refetch storm
