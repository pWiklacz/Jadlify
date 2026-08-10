---
date: 2026-07-18T12:45:00+02:00
researcher: Claude Fable 5
git_commit: 77ea034e9196dd0671c036d314e66f4eab205234
branch: feature/ui-redesign
repository: Jadlify
topic: "UI redesign — odtworzenie designu aplikacji z mockupów HTML w docs/design/"
tags: [research, codebase, ui-redesign, design-system, frontend, tailwind, react]
status: complete
last_updated: 2026-07-18
last_updated_by: Claude Fable 5
---

# Research: UI redesign — odtworzenie designu z mockupów `docs/design/`

**Date**: 2026-07-18T12:45:00+02:00
**Researcher**: Claude Fable 5
**Git Commit**: 77ea034e9196dd0671c036d314e66f4eab205234
**Branch**: feature/ui-redesign
**Repository**: Jadlify

## Research Question

Cały docelowy design aplikacji znajduje się w plikach HTML w katalogu `docs/design/` (7 mockupów `.dc.html` + `support.js` + `_plan-notes.md`). Mamy go odtworzyć w aplikacji jak najlepiej się da. Research ma przygotować grunt pod plan implementacji.

## Summary

**Stan wyjściowy:** wszystkie slice'y MVP (F-01…F-03, S-01…S-06) są `done` — pełny flow produktów, przepisów, celów, planu dnia i listy zakupów działa end-to-end. Frontend (`src/Jadlify.Web`: React 19 + Vite + Tailwind 3.4 + react-router 7 + TanStack Query 5) ma **czystą separację warstwy danych od prezentacji** — hooki `use*` per feature można zostawić nietknięte i wymienić wyłącznie markup. Obecny UI to surowy, jasny styl slate-on-white bez żadnych design tokenów (stockowy `tailwind.config.js`, zero fontów, zero wspólnych prymitywów UI — każdy przycisk/modal/karta to powtórzone ad-hoc klasy).

**Design docelowy:** ciemny, ciepły motyw (tło `#1F1712`, kremowe karty `#F8F1E3`, terakota `#C75B38`/`#D97E57`), fonty Archivo + Instrument Serif, pigułkowa nawigacja, spójny system komponentów (karty, modale/bottom-sheety, toasty, steppery, paski makro, skeleton/error/empty states) — wszystko po polsku, breakpoint mobilny 940px sterowany JS.

**Najważniejsze ustalenie — mockupy wykraczają poza zakres MVP.** Wierne odtworzenie designu to nie tylko restyling: mockupy zawierają widok tygodnia/miesiąca planu, przenoszenie/kopiowanie posiłków, trwałe listy zakupów z odhaczaniem i historią, dashboard z onboardingiem, rozszerzenia modelu produktu (marka, kategoria, opakowanie), porcje ułamkowe (0,5), wpisy planu z pojedynczych produktów oraz pełny pakiet kontowy (potwierdzenie e-mail, reset hasła, usunięcie konta). Część z nich to jawne Non-Goals PRD lub wymaga zmian w backendzie. **Plan musi rozstrzygnąć zakres** (patrz Open Questions) — rekomendowany podział: warstwa wizualna (restyling 1:1 możliwy od razu) vs. nowe funkcje (osobne decyzje/slice'y).

**Wpływ na testy:** ~14 plików testów Vitest+RTL i 1 spec Playwright lokalizują elementy po rolach i dostępnych nazwach (nie po CSS) — czysty restyling nie psuje prawie nic, ale **pełna migracja copy na polski łamie ~150+ asercji** (nazwy nagłówków, przycisków, labeli). To świadoma, jednorazowa migracja testów, którą plan musi zaplanować.

## Detailed Findings

### 1. Format mockupów (`.dc.html` + `support.js`)

Każdy plik `.dc.html` to samodzielny mockup: `<x-dc>` trzyma szablon HTML z interpolacjami `{{ }}`, a `<script type="text/x-dc">` definiuje `class Component extends DCLogic`, której `renderVals()` zwraca płaską mapę wartości/handlerów bindowaną do szablonu (runtime w `docs/design/support.js`, React na `window.React`). `<helmet>` wstrzykuje style globalne (Google Fonts, keyframes `jd-*`). `sc-if`/`sc-for` to warunki/pętle; `style-hover`/`style-focus` to pseudo-stany aplikowane przez runtime. Atrybut `data-props` opisuje panel scenariuszy demo (`scenariusz`, `stanSystemu`, `bladZapisu`…) — **enumeruje stany, które realna aplikacja musi obsłużyć**. Żaden z plików nie używa trybu canvas (wbrew `_plan-notes.md`); responsywność sterowana JS: `window.innerWidth <= 940` → `isMobile`.

### 2. Wspólny shell aplikacji (zweryfikowany we wszystkich plikach)

Strukturalnie identyczny w 6 plikach (wyjątek: Logowanie — patrz §4.7):
- **Header** (grid `1fr auto 1fr`, border-bottom `rgba(242,233,216,0.08)`): status pill z lewej (999px, 11px/700, ls 0.12em, kropka 7px), wyśrodkowane logo (listek SVG `#D97E57` 34px + „Jadlify" Instrument Serif 26px + tagline `PLANER POSIŁKÓW I MAKRO` 9px/700 ls 0.26em), z prawej okrągły przycisk konta 36px (aria-label „Moje konto") + link „Wyloguj".
- **Nawigacja pigułkowa** — 6 pozycji w stałej kolejności: `Strona główna · Produkty · Przepisy · Plan posiłków · Dzienne cele · Lista zakupów`. Pigułka `8px 14px / r999 / 13.5px`; aktywna `600/#F2E9D8/bg rgba(242,233,216,0.1)` + `aria-current="page"`.
- **Mobile**: sticky header (`z-30, bg #1F1712`), mniejsze logo bez tagline, nav jako poziomo przewijany rząd pigułek (aktywna bg `0.12`).
- **Status pill semantycznie** najlepiej rozwiązany w Liście zakupów (`BRAK AKTYWNEJ LISTY` / `ZAKUPY W TOKU` / `WSZYSTKO KUPIONE`) i na Stronie głównej (`W CELU` / `PONAD CEL` / `PUSTY PLAN` / `BRAK CELU`) — wersje „MAKIETA · …" w Produktach/Przepisach to artefakt makiety.

Obecny odpowiednik: [AppShell.tsx](src/Jadlify.Web/src/layout/AppShell.tsx) (biały top bar h-14, hamburger + drawer na mobile, `max-w-5xl`) + [navItems.ts](src/Jadlify.Web/src/layout/navItems.ts) (jedno źródło pozycji nav — redesign nav dotyka tylko renderowania w AppShell) + [AccountMenu.tsx](src/Jadlify.Web/src/layout/AccountMenu.tsx).

### 3. System designu — tokeny (delta względem `docs/design/_plan-notes.md`)

`_plan-notes.md` pokrywa bazę (fonty, tła, terakota, przyciski, karty, animacje). Agenci wyciągnęli istotne uzupełnienia:

**Palety funkccyjne:**
- Makro: białko `#C75B38`; węglowodany pasek/kafel `#C9A227`, chip `#A8861B`; tłuszcz pasek/kafel `#6D8B5C`, chip `#5F7C4E` (dwutonowość chip vs wypełnienie — do potwierdzenia/ujednolicenia).
- Zieleń (sukces): `#5F7C4E` (paski, checkboxy, toasty ok), `#4C6340` (tekst/badge), `#8FBF6A` (kropki), `#AFCB90` (tekst na ciemnym), tinty `rgba(95,124,78,0.10–0.14)`.
- Bursztyn (ostrzeżenia): tekst `#8A6414`/`#9A7B12`/`#6E5A12`, na ciemnym `#D9B25E`, tinty `rgba(201,162,39,0.1)` / `rgba(199,145,42,0.10–0.15)`.
- Czerwień (błędy/danger): `#B3402E` (znany) + `#A93A28`, `#8F3222`, `#96341F`, `#9C3626`, stonowany `#9B5A47`; border błędu zawsze `1.5px solid #B3402E`.
- Skala kremowa: sub-panel `#FCF7EC` (border `#EADFC8`), chip/hover `#EFE5D2`→`#E5DAC3`, track `#EBE0CB`, disabled fill `#D8CBB2`/`#E5D8C2`, disabled text `#7A6E5B`/`#A79A83`, ink ramp `#5C5140…#D8CBB2`.
- Ciemny przycisk wtórny: `#2A2018`, hover `#1F1712`/`#3A2C1F`. Overlaye: `rgba(18,12,8,0.6)` (dominujący) vs `rgba(31,23,18,0.6)` (plan).

**Pozostałe tokeny:** breakpoint **940px**; radiusy: karty 18–22, panele 14–16, wiersze 12–14, inputy 11–12, checkboxy 7–9, pigułki 999; touch targets 44–48px; `font-variant-numeric:tabular-nums` na wszystkich liczbach; `overflow-wrap:anywhere` na nazwach od użytkownika; skala z-index: sticky 25 / picker 35–40 / modal 50 / leave-dialog 55 / toast 60 / auth 80; cienie: modal `0 30px 80px rgba(0,0,0,0.5)`, toast `0 12px 32px rgba(0,0,0,0.4)`, FAB `0 14px 34px rgba(0,0,0,0.45)`; animacje `jd-pulse/spin/toast/rise/bar/ring/fade`; ring kcal: viewBox 120, r 52, stroke 9, obwód 326.7.

**Stan obecny:** [tailwind.config.js](src/Jadlify.Web/tailwind.config.js) całkowicie stockowy, [index.css](src/Jadlify.Web/src/index.css) tylko 3 dyrektywy `@tailwind`, [index.html](src/Jadlify.Web/index.html) bez fontów i z `lang="en"`. Cała warstwa tokenów to greenfield (Tailwind **v3.4** → tokeny przez `theme.extend`, nie v4 `@theme`).

### 4. Inwentarz ekranów (mockup → stan obecny)

#### 4.1 Strona główna (`Jadlify - Strona główna v5.dc.html`) — NAJWIĘKSZA LUKA
Dashboard wybranego dnia: nagłówek daty (H1 serif + badge `DZISIAJ/JUTRO/…` + data), hero **„Bilans dnia"** (SVG ring kcal 172px + 3 paski makro z kropkowanymi leaderami i copy „zostało N g" / „cel osiągnięty" / „przekroczono o N g" + 3 kafle statystyk), sekcja planu (4 karty grup posiłków Śniadanie/Obiad/Kolacja/Przekąska z wierszami posiłków: chipy makro, stepper porcji ±0,5, select typu, usuwanie), prawa szyna (karta aktywnej listy zakupów + karta „Następny krok" z kontekstowym CTA), **onboarding nowego użytkownika** (5-krokowa checklista Produkt→Przepis→Cele→Plan→Makro/zakupy) i baner „Dokończ konfigurację — krok X z 5", modal „Dodaj posiłek" (wyszukiwarka przepisów, segmenty typu, stepper porcji z walidacją, podgląd „PO DODANIU DO PLANU", tworzenie przepisu in-modal), toast (saving/ok/err/info).
**Obecnie:** [LandingPage.tsx](src/Jadlify.Web/src/routes/LandingPage.tsx) to `<h1>Home</h1>` + „Signed in as {userId}" — dashboard nie istnieje; wszystkie dane są dostępne przez istniejące hooki (`useMealPlan`, `useDailyMacroSummary`, `useDailyGoal`, `useShoppingList`).

#### 4.2 Plan posiłków (`Jadlify - Plan posiłków.dc.html`) — NAJWIĘKSZE ROZSZERZENIE ZAKRESU
Mockup to pojedynczy interaktywny ekran (bez wariantów 1a/1b z plan-notes) z przełącznikiem **Dzień / Tydzień / Miesiąc**, rzędem 7 kart dni (wybrana kremowa, dziś z ringiem `#D97E57` + badge `DZIŚ`, status ±5% tolerancji), chipami podsumowania tygodnia, panelem wybranego dnia (grupy posiłków + aside „Bilans dnia" + akcje **Przenieś / Kopiuj do innych dni / Usuń**), widokiem miesiąca (siatka 42 komórek), 6 modalami (dodaj/zastąp z trybem **Przepis/Produkt** — produkt dodawany gramaturą ±10 g; przenieś; kopiuj posiłek; kopiuj dzień z trybem add/replace i checkboxem potwierdzenia; utwórz listę zakupów; custom date-picker), toastem z **Cofnij** (undo delete), FAB na mobile, obsługą **niedostępnego przepisu** (wpis ze snapshotem `snap {name,kcal,b,t,w}` + „Zastąp innym przepisem"). Kontrakt cross-page: `localStorage['jadlify.planAddCtx']` + URL `?utworz=przepis|produkt&powrot=plan` → powrót z `?nowyPrzepis|nowyProdukt=…&kcal&b&t&w`. **Drag&drop NIE występuje w mockupie** (tylko dostępne przyciski). Uwaga: bug w mockupie `Plan:1364` (`#CDбир` → powinno być `#CDC0A6`) — nie kopiować.
**Obecnie:** [MealPlanPage.tsx](src/Jadlify.Web/src/planning/MealPlanPage.tsx) — pojedynczy dzień, input daty, formularz wpisu (select przepisu/typu, porcje **całkowite**), lista wpisów z edycją inline, [DailyMacroSummaryPanel.tsx](src/Jadlify.Web/src/planning/DailyMacroSummaryPanel.tsx). Widok tygodnia/miesiąca, przenoszenie/kopiowanie, wpisy produktowe i porcje 0,5 **nie istnieją** (część to Non-Goals PRD — patrz Open Questions).

#### 4.3 Dzienne cele (`Jadlify - Dzienne cele.dc.html`)
Trzy wzajemnie wykluczające się widoki: karta podsumowania (duży kcal serif `#C75B38`, 3 kafle makro ze swatchami, **notka spójności makro↔kcal**: `4b+4w+9t` vs cel, próg max(75, 5%); przyciski Edytuj/Usuń), empty state (checklista „co nadal działa"), formularz (KALORIE wyróżnione 56px, walidacja per-pole z `aria-invalid` i fokusem pierwszego błędu, **nieblokujące ostrzeżenie o niespójności makro**, modal niezapisanych zmian, modal usunięcia z listą konsekwencji). Aside: podgląd na żywo „PRZY OBECNYM PLANIE DNIA" (paski aktualizowane w trakcie pisania), karta „Gdzie używamy celów". Model: `goal {kcal, protein, fat, carbs} | null` + **`updatedAt`** (etykieta „Zaktualizowano…").
**Obecnie:** [DailyGoalsPage.tsx](src/Jadlify.Web/src/planning/DailyGoalsPage.tsx) — jeden formularz z 4 `NumberField`, upsert przez `useDailyGoal`. Brak: usuwania celu, podglądu na żywo, notki spójności, timestampu.

#### 4.4 Produkty (`Jadlify - Produkty.dc.html`)
4 ekrany wewnętrzne: index (toolbar: wyszukiwarka pigułkowa z czyszczeniem, select kategorii ×9, sortowanie ×4, licznik z polską odmianą; chipy aktywnych filtrów; grid kart `minmax(270px,1fr)`), szczegóły produktu (panel „NA 100 G" + wyliczone „CAŁE OPAKOWANIE", dodatkowe wartości odżywcze, „UŻYWANY W PRZEPISACH" + notka o snapshotach), formularz (sekcje: nazwa+**marka** [badge „ROZSZERZENIE MODELU"], kod kreskowy z 6 stanami lookupu [w tym **duplikat kodu** z akcją „Otwórz istniejący produkt"], 4 makra wymagane z badge'ami „DO UZUPEŁNIENIA" po częściowym lookupie, **kategoria + opakowanie**, zwijane 14 pól rozszerzonych w 4 grupach), dialog usunięcia (wariant zablokowany przez użycie w przepisach / wolny), guard niezapisanych zmian, mobile sticky save bar, ekran powrotu do szkicu przepisu.
**Obecnie:** [ProductsPage.tsx](src/Jadlify.Web/src/products/ProductsPage.tsx) + [ProductFormModal.tsx](src/Jadlify.Web/src/products/ProductFormModal.tsx) — CRUD z lookupem kodu i 14 polami rozszerzonymi już istnieje (EXTENDED_NUTRITION_KEYS w [types.ts](src/Jadlify.Web/src/products/types.ts)). Brak: wyszukiwarki/filtrów/sortowania, ekranu szczegółów, marki/kategorii/opakowania (zmiany modelu → backend), blokady usunięcia przez użycie, deduplikacji kodu.

#### 4.5 Przepisy (`Jadlify - Przepisy.dc.html`)
Index (wyszukiwarka, sort, karty z badge „W PLANIE"), szczegóły (panele „NA 1 PORCJĘ" + „CAŁY PRZEPIS", składniki z udziałem % kcal, akcja **„Dodaj do planu"**), formularz (stepper porcji int≥1, notka „gramatury dotyczą CAŁEGO przepisu", picker produktu jako dropdown-listbox z wyszukiwarką i disabled „JUŻ W PRZEPISIE", przenoszenie wierszy góra/dół, sticky aside „PODSUMOWANIE NA ŻYWO"), **inline formularz nowego produktu** (uproszczony: nazwa/marka/4 makra — pełny ekran zamiast zagnieżdżonego modala), modal „Dodaj posiłek" (porcje 0,5), dialogi usunięcia (zablokowany przez plan / wolny). Dwa różne empty states: brak produktów („Najpierw dodaj produkt", CTA disabled) vs brak przepisów.
**Obecnie:** [RecipesPage.tsx](src/Jadlify.Web/src/recipes/RecipesPage.tsx) + [RecipeFormModal.tsx](src/Jadlify.Web/src/recipes/RecipeFormModal.tsx) (z zagnieżdżonym ProductFormModal) + `MacroPanel` — funkcjonalnie bliskie; brak wyszukiwarki/sortowania, szczegółów, „Dodaj do planu" z poziomu przepisu, badge „W PLANIE" (409 `Recipe.InUse` już obsłużone).

#### 4.6 Lista zakupów (`Jadlify - Lista zakupów.dc.html`) — DRUGIE DUŻE ROZSZERZENIE
Mockup zakłada **trwałe listy**: index (karta aktywnej listy z postępem „X z Y kupione", historia „Poprzednie listy", badge `UKOŃCZONA`), kreator 2-krokowy (wybór dni z siatki 14 dni + quick-selecty; krok 2: nazwa listy, chipy podsumowania, zwijana personalizacja per-posiłek), tryb zakupów (3 widoki **Zakupy / Wg posiłków / Wg dni**, sticky pasek kontrolny, checkboxy kupione z przekreśleniem, grupowanie po kategoriach, expander „UŻYTO W", baner „Wszystko kupione", **modal 3-sekcyjnego diffa** po zmianie planu: DOJDĄ/ZNIKNĄ/ZMIENI SIĘ ILOŚĆ z zachowaniem stanu „kupione"). Mockup trzyma odhaczenia w `localStorage['jadlify-lz-v1']`.
**Obecnie:** [ShoppingListPage.tsx](src/Jadlify.Web/src/shopping/ShoppingListPage.tsx) — lista **compute-on-read** z `GET /api/shopping-list?date=` dla jednego dnia, tylko wyświetlanie (MVP celowo bez odhaczania — Non-Goal PRD). Trwałe listy, multi-dzień, odhaczanie, historia i diff wymagają backendu.

#### 4.7 Logowanie i konto (`Jadlify - Logowanie i konto.dc.html`)
Trzy layouty: **pre-auth** (dwukolumnowy: brand z value statement + kremowa karta 420px; formularze logowania [z „Pokaż/Ukryj" hasło], rejestracji [live checklista wymagań hasła ≥8/wielka+mała/cyfra, checkbox regulaminu], **potwierdzenia e-mail** [4 warianty + resend z cooldownem 45 s], **resetu hasła** [enumeration-safe copy], ustawienia nowego hasła [linki wygasłe/zużyte]), **karty centralne** (sesja wygasła z „Wrócisz do: …", wylogowano, konto usunięte), **app shell konta** (Ustawienia: karty KONTO / BEZPIECZEŃSTWO [zmiana hasła — modal z notą o wylogowaniu innych sesji] / **STREFA NIEODWRACALNA** [usunięcie konta z type-to-confirm „USUŃ"]). Uwaga: shell tego pliku różni się od pozostałych (nav bez „Plan posiłków", avatar „KN" zamiast ikony, inne style inputów) — do ujednolicenia przy implementacji; copy w rodzaju żeńskim (persona) — do neutralizacji.
**Obecnie:** [LoginPage.tsx](src/Jadlify.Web/src/routes/LoginPage.tsx) (signin/signup toggle, polska walidacja, [authErrors.ts](src/Jadlify.Web/src/auth/authErrors.ts)) + AccountMenu „Wyloguj". Brak: potwierdzeń e-mail (Supabase confirmations **wyłączone** decyzją S-01), resetu/zmiany hasła, ustawień konta, usunięcia konta (PRD ma NFR „Prawo do usunięcia" bez UI).

### 5. Komponenty współdzielone do wyodrębnienia (z obu analiz mockupów)

Powtarzające się w 6–7 plikach — naturalna biblioteka prymitywów dla redesignu:
1. **AppShell** (header + status pill + pill nav, desktop/mobile).
2. **Toast** (dolna pigułka, `role=status`, warianty saving/ok/err/info + opcjonalne Cofnij/Spróbuj ponownie) — ujednolicić rozjazdy kolorów/czasów między plikami.
3. **Modal/Dialog** (desktop centered `min(560–640px,100%)` r22 / mobile bottom-sheet `r 20 20 0 0`; header serif + okrągły close 44px, scroll-body, footer Anuluj+primary; backdrop-click; focus trap) — obecnie logika modala jest skopiowana w 4 plikach ([ProductFormModal.tsx](src/Jadlify.Web/src/products/ProductFormModal.tsx), DeleteProductDialog, RecipeFormModal, DeleteRecipeDialog) — redesign to moment na ekstrakcję.
4. **MacroCompareRow** (label + kropkowany leader + `val / goal` + pasek 5–7px + status tekstowy) — 5 wariantów w mockupach.
5. **Stepper** (−/wartość/+ w pigułce; kroki 0,5 / 1 / 10 g).
6. **Karta encji** (krem r18, serif nazwa, duża liczba serif + uppercase podpis, linia makro, stopka z updated + ikony edycji/usunięcia).
7. **Skeleton loader** („Wczytujemy Twoje …" + jd-pulse), **karta błędu pobierania** (z copy „Twoje dane są bezpieczne" + „Wczytaj ponownie"), **empty state** (ikona, serif H2, CTA).
8. **Field kit formularzy** (uppercase labels 11px ls 0.16em, inputy `#FCF7EC` r12, błędy „▲ …" `role=alert`, decimal z przecinkiem, „(OPCJONALNIE)").
9. **Bannery** (info/warning/success/error — jedna anatomia).
10. **Selektory dni/dat** (checklista dni, mini-kalendarz miesiąca, custom date-picker dialog).
11. **Guard niezapisanych zmian** + dialogi potwierdzenia usunięcia (wariant zablokowany/wolny).
12. **Helpery PL**: `pol(n, jeden, dwa, pięć)` (porcja/porcje/porcji, posiłek/posiłki/posiłków…), `Intl` pl-PL, przecinek dziesiętny, `parseNum(",")` — używane wszędzie.

### 6. Rozbieżności między mockupami (do rozstrzygnięcia raz, w planie)

- **`max-width` main**: 1180 (główna) / 1240 (plan) / 1120 (cele) / 1140 (produkty, przepisy) / 1080 (lista) — przyjąć skalę lub jedną wartość.
- **Kolory/czasy toastów**: ok `#5F7C4E` vs `#4C6340` vs ciemny `#2A2018`; err `#A93A28` vs `#B3402E` vs `#8F3222`; autohide 2,8/3,0/3,5/5,2 s.
- **Klucze makro**: `b/t/w` (główna, plan) vs `protein/fat/carbs` (cele) — API używa pełnych nazw; ujednolicić w warstwie UI.
- **Taksonomia kategorii**: Produkty `zbozowe/sypkie/mrozonki/napoje/inne` vs Lista `zboza/spizarnia` (bez 3 ostatnich) — zunifikować.
- **Tworzenie przepisu w flow dodawania posiłku**: in-modal (główna) vs nawigacja z handoffem localStorage (plan) — wybrać jeden wzorzec.
- **Etykiety**: „+ Dodaj śniadanie" (główna) vs „+ Dodaj — śniadanie" (plan).
- **Nawigacja po datach**: natywny `<input type=date>` (główna) vs custom popover kalendarza (plan — nowszy, bogatszy wzorzec).
- **Progi statusu celu**: ±5% tolerancji (plan) vs dokładna równość (główna, cele).
- **Overlay**: `rgba(18,12,8,0.6)` vs `rgba(31,23,18,0.6)`; dwutonowość kolorów makro chip vs pasek.
- Martwy kod mockupów (nie przenosić): stare wartości karty zakupów na stronie głównej, inline kreatory w modalu planu, scenario-rail w Logowaniu, bug `#CDбир` (`Plan:1364`).

### 7. Wpływ na testy

- **Vitest + RTL** (14 plików, ~1800 linii): wszystkie lokalizują po `getByRole`/`getByLabelText`/tekście. Czyste testy logiki (client, authErrors, macroMath, SessionProvider) — nietknięte. Testy stron ([ProductsPage.test.tsx](src/Jadlify.Web/src/products/ProductsPage.test.tsx) ~55 asercji, RecipesPage ~43, MealPlanPage ~39, AppShell, LoginPage, DailyGoalsPage, ShoppingListPage) — **złamie je zmiana copy na polski i zmiany struktury nav** (AppShell.test zakłada drawer + aria-label „Main"/„Mobile"; karty jako `listitem`; dialogi z `aria-labelledby`).
- **Playwright**: [seed.spec.ts](src/Jadlify.Web/tests/e2e/seed.spec.ts) — flow produktu na `getByRole`/`getByLabel` z angielskim copy („Products", „Add product", „Calories (kcal / 100 g)") — do migracji razem z copy.
- **Zasady projektu** (CLAUDE.md, test-plan): locatory rolowe, bez `waitForTimeout`, bez snapshotów layoutu; **zachować semantyczne role/nazwy i konwencję warunkowego montowania** (jsdom nie ewaluuje media queries — komentarz w [AppShell.tsx:11-14](src/Jadlify.Web/src/layout/AppShell.tsx)); mockupy używają JS-owego breakpointu 940px (matchMedia), co jest zgodne z tą konwencją.
- Meal types to surowe wartości enum API (`Breakfast/Lunch/Dinner/Snack` w [planning/types.ts:30](src/Jadlify.Web/src/planning/types.ts)) — polskie etykiety wymagają **mapy etykiet**, nie zmiany wartości wire.

## Code References

- `docs/design/_plan-notes.md` — wyciągnięte tokeny bazowe (fonty, kolory, layout, animacje)
- `docs/design/Jadlify - Plan posiłków.dc.html:1024-1051` — kontrakt handoffu localStorage + URL params między stronami
- `docs/design/Jadlify - Plan posiłków.dc.html:1364` — bug mockupu (`#CDбир`), nie kopiować
- `docs/design/Jadlify - Produkty.dc.html:425` — marka jako „ROZSZERZENIE MODELU" (wymaga backendu)
- `docs/design/Jadlify - Produkty.dc.html:1010` — regex kodu kreskowego `^[0-9]{8,14}$`
- `docs/design/Jadlify - Lista zakupów.dc.html:881-898` — persystencja odhaczania w localStorage
- `docs/design/Jadlify - Logowanie i konto.dc.html:546` — reguły hasła; `:466` — modal usunięcia konta
- `src/Jadlify.Web/src/App.tsx:12-36` — router (wszystkie trasy)
- `src/Jadlify.Web/src/layout/AppShell.tsx:16-95` — obecny shell (do wymiany na pill nav)
- `src/Jadlify.Web/src/layout/navItems.ts:14-21` — jedno źródło pozycji nawigacji
- `src/Jadlify.Web/src/routes/LandingPage.tsx:8-36` — pusty dashboard (największa luka)
- `src/Jadlify.Web/tailwind.config.js`, `src/Jadlify.Web/src/index.css`, `src/Jadlify.Web/index.html` — puste miejsce na tokeny/fonty (`lang="en"` do zmiany)
- `src/Jadlify.Web/src/api/client.ts:29-69` + `src/api/apiClient.ts:12` — warstwa API (nie dotykać)
- `src/Jadlify.Web/src/products/ProductFormModal.tsx:158-198` — ręczny focus trap (duplikowany ×4 → ekstrakcja Modal)
- `src/Jadlify.Web/src/planning/types.ts:30` — enum meal types (wire values, mapować etykiety)
- `src/Jadlify.Web/tests/e2e/seed.spec.ts:35-72` — jedyny spec e2e (locatory rolowe, EN copy)

## Architecture Insights

1. **Prezentacja jest czysto odseparowana od danych** — hooki TanStack Query per feature (`useProducts`, `useRecipes`, `useMealPlan(date)`, `useDailyGoal`, `useShoppingList(date)`…) zwracają standardowe wyniki; zero fetchy w JSX. Redesign wizualny = wymiana markupu bez dotykania `api/`, `auth/`, `use*.ts`.
2. **Konwencja feature-folderów** (`src/<feature>/{types.ts, use*.ts, *Page.tsx, *Modal.tsx}` + cienkie wrappery w `routes/sections/`) — utrzymać; nowe współdzielone prymitywy powinny trafić do nowego folderu (np. `src/ui/`), którego dziś nie ma.
3. **Brak jakiejkolwiek biblioteki prymitywów UI** — redesign bez wcześniejszej ekstrakcji Button/Card/Input/Modal/Toast oznacza restylowanie ~20 plików zduplikowanych stringów klas; ekstrakcja najpierw znacząco zmniejsza ryzyko dryfu.
4. **Dostępność jako kontrakt testowy**: role, aria-atrybuty, focus trap, `role=status/alert` są tym, po czym testy (i przyszłe e2e wg lekcji 10x) lokalizują elementy. Mockupy są pod tym względem solidne (aria-labels, 44px targets, `aria-current`, `role=dialog`) — przenosić 1:1.
5. **Constraint deploymentu**: SPA statycznie budowana do `wwwroot` API (jedna domena, fallback `index.html`) — żaden element redesignu nie może wymagać SSR.
6. **Backend jest autorytatywny dla makro** — podglądy na żywo (formularz przepisu, modal dodawania posiłku, podgląd celów) liczą client-side wyłącznie jako preview (wzorzec z `macroMath.ts`, 1 miejsce po przecinku via `formatMacro`).
7. **Mockupy definiują budżet UX stanów przejściowych**: symulowane opóźnienia 550–900 ms zapisu z toastem saving→ok/err, skeletony przy ładowaniu, strip „odświeżanie w tle" — spójne z NFR 200 ms/2 s (realizowanym przez stany TanStack Query).

## Historical Context (from prior changes)

- `context/foundation/prd.md` — 16 FR must-have; FR-013 wymaga **liczbowej** delty vs cel (paski wolno dodać, liczb nie wolno usunąć); Non-Goals: plan tygodniowy, drag&drop, eksport listy, skaner aparatem, historia celów, odhaczanie listy.
- `context/foundation/roadmap.md` — wszystkie slice'y `done`; redesign to praca cross-cutting poza sekwencją slice'ów.
- `context/foundation/test-plan.md` — brak snapshotów layoutu (celowo); warstwa e2e w fazie 3 (nierozpoczęta); locatory rolowe.
- `context/archive/2026-05-29-responsive-app-shell/plan.md` — fundamenty frontendu: Vite+React+TS+Tailwind, RequireAuth, SessionProvider, apiClient z żywym tokenem, TanStack Query jako mechanizm NFR responsywności.
- `context/archive/2026-05-30-account-sign-in-flow/plan.md` — dyscyplina stanu auth (redirecty przez `onAuthStateChange`, nie imperatywnie), `toAuthMessage()` nie echo'uje credentiali, **potwierdzenia e-mail Supabase wyłączone** (mockup konta zakłada włączone — decyzja do podjęcia).
- `context/archive/2026-05-31-product-catalog-with-barcode-fallback/plan.md` — wzorzec danych (query+invalidate), lookup kodu server-side, fallback ręczny to nie błąd.
- `context/archive/2026-05-31-recipe-builder-with-macro-calculation/plan.md` — preview client-side/backend autorytatywny, snapshoty składników, `409 Recipe.InUse`.
- `context/archive/2026-06-02-daily-goals-and-meal-plan/plan.md` — **porcje całkowite**, edycja tylko typ+porcje, jeden aktualny cel, klucze query z datą ISO.
- `context/archive/2026-06-04-daily-macro-summary/plan.md` — podsumowanie na `/meal-plan` (nie osobna trasa), „remaining" per makro, jawnie POZA zakresem: paski/procenty/subtotale per typ (mockupy je dodają — świadome rozszerzenie).
- `context/archive/2026-06-06-shopping-list-from-day-plan/plan.md` — lista compute-on-read, agregacja po `ProductId`, sort A–Z, gramy 1 miejsce po przecinku.
- `context/foundation/lessons.md` — nie istnieje.
- `mvp-check-report-2026-07-18.md` — 5/5 technicznie, ocena jawnie **wyłączyła warstwę wizualną** — redesign adresuje dokładnie tę lukę; kontrakty API stabilne.

## Related Research

- Brak wcześniejszych plików `research.md` w `context/changes/**` ani `context/archive/**` (to pierwszy artefakt research w projekcie).

## Open Questions

Decyzje zakresu, które plan (`/10x-plan`) musi rozstrzygnąć — proponowany podział na warstwy:

1. **Warstwa 0 — czysty restyling (bez zmian API):** tokeny + fonty + shell + prymitywy UI + restyling istniejących ekranów (produkty, przepisy, cele, plan jednodniowy, lista jednodniowa, logowanie) + polska lokalizacja copy + migracja testów. Możliwe od zaraz. Czy tak dzielimy?
2. **Dashboard (Strona główna):** budowa od zera na istniejących hookach — dołączyć do redesignu (rekomendacja: tak, to serce nowego designu) czy osobny change?
3. **Plan tygodnia/miesiąca + Przenieś/Kopiuj/Kopiuj dzień:** Non-Goals PRD; wymagają nowych endpointów (batch odczyt zakresu dat, operacje move/copy). W zakresie redesignu, osobny change, czy odpuścić na razie?
4. **Trwałe listy zakupów** (odhaczanie, historia, kreator wielodniowy, diff po zmianie planu): duży backend (nowa encja). MVP miał listy display-only. Decyzja jak wyżej; ewentualnie kompromis: odhaczanie w localStorage (jak w mockupie) bez backendu?
5. **Rozszerzenia modelu produktu** (marka — w mockupie jawnie oznaczona jako rozszerzenie; kategoria; opakowanie; blokada usunięcia gdy używany; deduplikacja kodu): backend + migracje. Zakres?
6. **Porcje ułamkowe (0,5)** w planie: obecnie int-only (decyzja S-04, walidacja backendu). Zmieniamy kontrakt?
7. **Wpisy planu z pojedynczego produktu** (pid+grams zamiast przepisu): nowa funkcja backendu. Zakres?
8. **Pakiet kontowy** (potwierdzenie e-mail — wymaga włączenia w Supabase; reset hasła; zmiana hasła; usunięcie konta — NFR „Prawo do usunięcia"): który podzbiór?
9. **Niedostępny przepis ze snapshotem** w planie: obecnie usunięcie przepisu jest blokowane 409, więc stan „gone" nie występuje — utrzymać blokadę (prościej) czy przejść na snapshoty (jak mockup)?
10. **Ujednolicenia z §6** (max-width, toasty, taksonomia kategorii, wzorzec tworzenia w flow, progi statusu) — do rozstrzygnięcia w planie jako tabela decyzji.
