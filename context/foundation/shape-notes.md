---
project: "Jadlify"
context_type: greenfield
created: 2026-05-20
updated: 2026-05-20
product_type: web-app
target_scale:
  users: small
  qps: low
  data_volume: small
timeline_budget:
  mvp_weeks: 4
  hard_deadline: null
  after_hours_only: true
checkpoint:
  current_phase: 8
  phases_completed: [1, 2, 3, 4, 5, 6, 7]
  gray_areas_resolved:
    - topic: "pain category"
      decision: "wszystkie cztery: tarcie operacyjne + dane uwięzione + brak narzędzia decyzyjnego + paraliż decyzyjny przy zakupach/meal prep"
    - topic: "product insight"
      decision: "planowanie posiłków z wyprzedzeniem (vs śledzenie post-factum w MyFitnessPal/Fitatu); meal prep + makro jako pierwszorzędna pętla"
    - topic: "persona scope MVP"
      decision: "single-user (autor projektu) w MVP; szersza grupa docelowa (osoby planujące dietę dla zdrowia, hobby, treningu) jako kierunek po MVP — terminologia w produkcie nie ma hardkodować >>trening<<"
    - topic: "auth model"
      decision: "indywidualne konta z izolacją danych; konkretny mechanizm logowania zostaje decyzją techniczną po PRD"
    - topic: "user roles"
      decision: "flat — wszyscy użytkownicy równi, brak ról w MVP (zgodnie z dokumentem sekcja 7: brak ról dietetyk-pacjent)"
    - topic: "MVP scope-down"
      decision: "interfejs konsolowy, eksport plików i tagi przepisów wycięte z MVP (wracają w etapie 2 roadmapy). Uzupełnianie produktu po kodzie kreskowym ZOSTAJE. Agregacja listy zakupów ZOSTAJE."
    - topic: "MVP timeline"
      decision: "4 tygodnie after-hours; brak twardego deadline'u"
    - topic: "secondary outcomes"
      decision: "plan tygodniowy (7-dniowy), lista zakupów agregująca cały tydzień, powtórne użycie tego samego przepisu w wielu dniach"
    - topic: "guardrails"
      decision: "izolacja danych per-user; deterministyczna i poprawna kalkulacja makro; niedostępność danych po kodzie kreskowym nie blokuje user'a (zawsze można dodać ręcznie)"
  frs_drafted: 16
  quality_check_status: accepted
---

# Jadlify — shape notes

Notatki shapingowe wygenerowane przez `/10x-shape`. Źródłem seedowym jest `docs/jadlify-koncepcja-mvp.md`. Pełni rolę wejścia do `/10x-prd`.

---

## Vision & Problem Statement

Autor projektu trenuje siłowo i prowadzi dietę z trzymanymi makroskładnikami. Ból odczuwa cyklicznie w dwóch konkretnych momentach: gdy układa plan posiłków na kilka tygodni do przodu i gdy idzie do sklepu po składniki. Ręczne liczenie kcal i makro, ręczne przepisywanie składników z przepisów, brak jednego miejsca, w którym widać "co i kiedy będzie potrzebne", oraz brak narzędzia, które trafia w założone makro za niego — to są źródła tarcia. Status quo (MyFitnessPal, Fitatu, Excel) śledzi to, co już zostało zjedzone; nie pomaga planować z wyprzedzeniem ani składać listy zakupów spinającej cały plan.

Insight, który czyni ten produkt nie-pochodną tych narzędzi: zaplanowanie posiłków z wyprzedzeniem (z deklaratywną gramaturą składników) sprawia, że makra trafiają same, a lista zakupów wynika z planu — nie odwrotnie. Aplikacja traktuje plan posiłków i listę zakupów jako pierwszorzędne wyjścia, a śledzenie kalorii jest tylko ich konsekwencją.

---

## User & Persona

**Primary persona:** autor projektu (single-user w MVP). Osoba trenująca, dbająca o makroskładniki, planująca posiłki wielotygodniowo, robiąca meal prep i zakupy raz na 1–2 tygodnie. Sięga po aplikację w dwóch momentach: (a) siadając do ułożenia planu na kolejne dni/tygodnie, (b) przed wyjściem do sklepu — żeby mieć listę zakupów wynikającą z planu.

**Future scope (poza MVP):** szersza grupa osób planujących dietę — dla zdrowia, hobbystycznie, na redukcji/masie. Terminologia produktu nie ma być wąsko "treningowa", żeby nie zamykać tej drogi.

---

## Success Criteria

### Primary

Użytkownik kończy pełny przepływ end-to-end w jednej sesji:

1. zakłada konto i loguje się,
2. dodaje co najmniej jeden produkt — ręcznie LUB z pomocą kodu kreskowego,
3. tworzy co najmniej jeden przepis z gramaturami składników,
4. ustawia dzienne cele (kcal + białko/tłuszcz/węglowodany),
5. dodaje przepis(y) do planu wybranego dnia (z typem posiłku: śniadanie/obiad/kolacja/przekąska),
6. widzi podsumowanie kcal i makro dla tego dnia oraz różnicę względem celu,
7. generuje listę zakupów z planu (agregacja składników) i widzi ją na ekranie.

### Secondary

Mile widziane, ale nie blokują uznania MVP za sukces:

- plan tygodniowy (7-dniowy widok) z sumami per-dzień i per-tydzień,
- lista zakupów agregująca składniki z całego planu wielodnia (np. tygodniowego),
- powtórne użycie tego samego przepisu w wielu dniach planu (Σ składników skaluje się odpowiednio).

### Guardrails

Rzeczy, które nie mogą się zepsuć — failure tu jest regresją nawet jeśli Primary trzyma:

- **Izolacja danych per-user:** zasoby (produkty, przepisy, plany, cele, listy) jednego użytkownika nigdy nie są widoczne dla innego użytkownika ani niezalogowanego ruchu.
- **Deterministyczna i poprawna kalkulacja makro:** dla danego zestawu produkt-gramatura wynik liczenia kcal/białka/tłuszczu/węglowodanów jest zawsze ten sam i odpowiada manualnemu przeliczeniu na proporcji 100g.
- **Odporność uzupełniania produktu po kodzie kreskowym:** gdy dane dla kodu są niedostępne, błędne lub niepełne, użytkownik nadal może dodać produkt ręcznie wypełniając formularz. Dane z kodu są sugestią, nie blokerem.

---

## User Stories

### US-01: Przejście pełnego flow MVP w jednej sesji

- **Given** użytkownik z założonym kontem, brak istniejących produktów/przepisów/planów
- **When** użytkownik kolejno: (1) loguje się, (2) dodaje produkt po kodzie kreskowym lub ręcznie, gdy dane nie zostaną znalezione, (3) tworzy przepis z co najmniej jednym składnikiem, (4) ustawia dzienne cele kcal/makro, (5) dodaje przepis do dziś z typem posiłku, (6) otwiera widok podsumowania dnia, (7) generuje listę zakupów
- **Then** każdy z tych kroków kończy się sukcesem bez ręcznego obejścia, a użytkownik kończy sesję z wygenerowaną listą zakupów odpowiadającą jego planowi

#### Acceptance Criteria
- Wartości makro w podsumowaniu dnia odpowiadają sumie składników z każdego przepisu w planie, przeliczone proporcjonalnie do gramatur
- Lista zakupów zawiera wszystkie produkty z każdego przepisu dnia, zagregowane (te same produkty zsumowane gramaturami)
- Różnica między planem dnia a celem dziennym jest pokazana liczbowo dla każdego z 4 wymiarów (kcal, B, T, W)

### US-02: Dodanie produktu po kodzie kreskowym z fallbackiem

- **Given** zalogowany użytkownik
- **When** użytkownik wpisuje kod kreskowy
- **Then** aplikacja próbuje wypełnić formularz danymi produktu dla podanego kodu i — w zależności od wyniku — pokazuje formularz wypełniony do edycji albo pusty formularz z informacją o braku danych

#### Acceptance Criteria
- Gdy komplet danych makro jest dostępny: formularz jest wypełniony, użytkownik może zmienić każde pole przed zapisem
- Gdy dostępne są tylko częściowe dane: brakujące pola są puste, użytkownik je uzupełnia
- Gdy dane są niedostępne lub wystąpi błąd: formularz jest pusty, kod kreskowy jest już wpisany, użytkownik wypełnia ręcznie i zapisuje
- Niezależnie od ścieżki, zapisany produkt jest własnością tego użytkownika

### US-03: Wygenerowanie listy zakupów z agregacją

- **Given** zalogowany użytkownik z co najmniej jednym przepisem dodanym do planu jakiegoś dnia, gdzie kilka przepisów dzieli te same produkty
- **When** użytkownik klika "Generuj listę zakupów" dla tego dnia
- **Then** widzi listę produktów, gdzie każdy produkt występuje raz, z sumą gramatur ze wszystkich przepisów

#### Acceptance Criteria
- Dwa przepisy używające 100g i 50g tego samego produktu produkują na liście jeden wpis z 150g
- Lista nie zawiera duplikatów
- Lista jest widoczna na ekranie (eksport do pliku poza MVP)

---

## Functional Requirements

### Autentykacja i konto

- FR-001: Użytkownik może założyć konto z indywidualnym uwierzytelnianiem. Priority: must-have
  > Socratic: brak silnego kontrargumentu; trzymamy bez modyfikacji.
- FR-002: Użytkownik może się zalogować i wylogować. Priority: must-have
  > Socratic: brak silnego kontrargumentu; trzymamy bez modyfikacji.

### Produkty

- FR-003: Zalogowany użytkownik może dodać produkt ręcznie z nazwą i wartościami odżywczymi na 100g (kcal, białko, tłuszcz, węglowodany). Priority: must-have
  > Socratic: brak silnego kontrargumentu w obecnym scope. Otwarte pytanie o jednostki miary (g/ml/szt) — pójdzie do Open Questions; obecnie zakładamy gramy wszędzie.
- FR-004: Zalogowany użytkownik może wpisać kod kreskowy i otrzymać wstępnie uzupełniony formularz produktu; ma możliwość poprawienia danych przed zapisem oraz wypełnienia ręcznego, gdy dane są niedostępne lub niepełne. Priority: must-have
  > Socratic: Counter-argument: "uzupełnianie po kodzie kreskowym to dodatkowy zewnętrzny failure — wytnij z MVP". Resolution: trzymamy, ale FR-004 musi zawsze fallback'ować do FR-003 (brak danych po kodzie ≠ blokada). Guardrail "Odporność uzupełniania produktu po kodzie kreskowym" z Success Criteria to zabezpiecza.
- FR-005: Zalogowany użytkownik widzi listę swoich produktów. Priority: must-have
- FR-006: Zalogowany użytkownik może edytować lub usunąć własny produkt. Priority: must-have
  > Socratic: Counter-argument: "usunięcie produktu psuje przepisy, które go używają". Resolution: zachowanie zależnych przepisów i planów po usunięciu produktu — pójdzie do Open Questions.

### Przepisy

- FR-007: Zalogowany użytkownik może stworzyć przepis składający się ze składników (produkt + gramatura) i liczby porcji, na którą przepis jest skalowany. Priority: must-have
  > Socratic: Counter-argument: "przepis bez liczby porcji jest jednorazowy — nie obsługuje meal prep, kiedy user robi 4 porcje na raz". Resolution: przepis zawiera liczbę porcji; FR-008 liczy makro per-porcję i sumarycznie; w planie posiłków user może wskazać ile porcji bierze (FR-012).
- FR-008: Aplikacja automatycznie liczy kcal i makro przepisu — zarówno sumarycznie dla całego przepisu, jak i per porcję. Priority: must-have
  > Socratic: Counter-argument: "straty w gotowaniu (parowanie, redukcja) zmieniają wynik". Resolution: trzymamy makro z surowych składników — to standard branżowy; obróbka jest poza-MVP.
- FR-009: Zalogowany użytkownik widzi listę swoich przepisów. Priority: must-have
- FR-010: Zalogowany użytkownik może edytować lub usunąć własny przepis. Priority: must-have
  > Socratic: Counter-argument: "usunięcie przepisu psuje wpisy w planie, które go używają". Resolution: zachowanie zależnych wpisów planu po usunięciu przepisu — pójdzie do Open Questions.

### Cele

- FR-011: Zalogowany użytkownik może ustawić swoje dzienne cele kaloryczne (kcal, białko, tłuszcz, węglowodany). Priority: must-have
  > Socratic: brak silnego kontrargumentu. MVP zakłada jeden aktualny zestaw celów; historia/cykle (redukcja vs masa) po MVP.

### Plan posiłków

- FR-012: Zalogowany użytkownik może dodać przepis do planu wybranego dnia, wskazując typ posiłku (śniadanie/obiad/kolacja/przekąska) oraz liczbę porcji z tego przepisu. Ten sam przepis może wystąpić wielokrotnie w jednym dniu (różne typy posiłku) i w wielu dniach (meal prep). Priority: must-have
  > Socratic: Counter-argument: "typy posiłku to nadmiar struktury, wystarczy lista przepisów per dzień". Resolution: typy zostają — są w dokumencie i pomagają w czytelności. Counter-argument: "user chce duplikować przepis (meal prep)". Resolution: explicite dopuszczamy wielokrotne wystąpienie tego samego przepisu.
- FR-013: Zalogowany użytkownik widzi podsumowanie kcal i makro dla wybranego dnia oraz różnicę względem celu (liczbowo dla kcal/B/T/W). Priority: must-have
  > Socratic: Counter-argument: "różnica liczbowa jest mniej użyteczna niż paski postępu". Resolution: MVP daje liczby (must-have); paski / wizualizacja procentu — nice-to-have, po MVP.
- FR-014: Zalogowany użytkownik może edytować lub usunąć wpis z planu (zmiana typu posiłku lub liczby porcji bez konieczności usunięcia i dodania wpisu ponownie). Priority: must-have
  > Socratic: Counter-argument: "bez edycji user musi usuwać i dodawać wpis ponownie przy każdej zmianie porcji — pogarsza UX dla meal prep". Resolution: edycja dorzucona do MVP, bo meal prep wymaga częstej regulacji porcji.

### Lista zakupów

- FR-015: Zalogowany użytkownik może wygenerować listę zakupów z planu (jeden dzień), z agregacją tych samych produktów (sumowanie gramatur dla powtarzających się produktów). Priority: must-have
  > Socratic: Counter-argument: "agregacja wymaga jednostek miary (5 jajek + 250g jajek)". Resolution: MVP zakłada wszystkie produkty w gramach (FR-003), więc agregacja jest spójna. Jednostki miary (g/ml/szt) — Open Question na potem.
- FR-016: Zalogowany użytkownik widzi wygenerowaną listę zakupów na ekranie. Priority: must-have
  > Socratic: Counter-argument: "lista bez odhaczania pozycji jest trudna w sklepie". Resolution: checkboxy / interaktywna lista — nice-to-have, po MVP; eksport do pliku również poza MVP (decyzja scope-down z Fazy 3).

---

## Non-Functional Requirements

- **Czas odpowiedzi:** widoki list produktów / przepisów oraz podsumowanie dnia odpowiadają w czasie < 800 ms p95 dla normalnego użycia (do ~1000 produktów i ~200 przepisów na konto).
- **Responsywność dla użytkownika:** każda interakcja użytkownika daje widoczne potwierdzenie w ciągu 200 ms, a operacje trwające > 2 s pokazują ciągły, widoczny wskaźnik postępu.
- **Odporność uzupełniania produktu po kodzie kreskowym:** niedostępność lub niepełność danych dla kodu kreskowego nigdy nie blokuje użytkownika — pozostaje droga ręcznego dodania produktu i jest ona w pełni funkcjonalna.
- **Izolacja danych:** żaden zasób (produkt, przepis, plan, cel, lista zakupów) jednego użytkownika nie jest dostępny dla innego użytkownika ani dla ruchu niezalogowanego — niezależnie od używanego interfejsu produktu.
- **Prywatność operacyjna:** hasła i tokeny uwierzytelniające nigdy nie pojawiają się w logach, error trace'ach ani komunikatach diagnostycznych.
- **Prywatność produktowa:** w MVP aplikacja nie zbiera i nie wysyła danych do narzędzi analityki strony trzeciej — wprowadzenie takich narzędzi to świadoma decyzja, nie default.
- **Prawo do usunięcia:** użytkownik może zażądać usunięcia swojego konta wraz ze wszystkimi powiązanymi danymi; po realizacji żądania jego dane nie są dostępne przez normalne ścieżki produktu.
- **Wsparcie urządzeń i przeglądarek:** aplikacja jest użyteczna na dwóch ostatnich głównych wersjach Chrome / Firefox / Safari / Edge na desktopie oraz na najnowszych wersjach Safari iOS i Chrome Android — w obu trybach (desktop + responsive mobile).
- **Determinizm kalkulacji:** dla identycznego zestawu produkt-gramatura-porcje wynik liczenia kcal/B/T/W jest zawsze ten sam i odpowiada manualnemu przeliczeniu wartości z proporcji 100g.

---

## Business Logic

**Aplikacja porównuje plan posiłków użytkownika z jego dziennymi celami kalorycznymi i makroskładnikowymi, oraz — na podstawie tego samego planu — przedstawia zagregowaną listę składników do kupienia, żeby użytkownik mógł świadomie skorygować plan zanim wyjdzie do sklepu.**

Reguła łączy dwie konsekwencje jednego wejścia (planu posiłków):

- **Decyzja kalibracyjna:** aplikacja zwraca użytkownikowi rzeczywiste kcal / białko / tłuszcze / węglowodany jego planu dnia oraz różnicę względem ustawionych celów. Wartości są liczone w sposób przejrzysty i deterministyczny: składnik wnosi do sumy wartości proporcjonalne do gramatury, przepis wnosi wartości proporcjonalne do wybranej liczby porcji, dzień jest sumą wszystkich wpisów planu.
- **Konsekwencja zakupowa:** ten sam plan, przeczytany od strony składników (a nie kalorii), staje się listą zakupów — z agregacją po identyfikatorze produktu, żeby ten sam produkt nie pojawiał się dwa razy.

Użytkownik styka się z regułą w jednym miejscu: na widoku dnia widzi (1) zsumowane makro vs cel oraz (2) wynikającą z planu listę zakupów. Jeśli plan nie trafia w cele, użytkownik koryguje go ręcznie (zmienia liczbę porcji, dorzuca/usuwa przepis) — a aplikacja natychmiast pokazuje nowy stan obu wyjść.

Reguła jest opisana bez nazwania komponentów, które ją realizują; miejsce wykonania obliczeń jest decyzją techniczną po PRD.

---

## Access Control

Model: każdy użytkownik ma własne konto z indywidualnym logowaniem; wybór dostawcy i mechanizmu logowania jest decyzją techniczną po PRD. Wszystkie zasoby — produkty, przepisy, plany posiłków, cele kaloryczne, listy zakupów — są przypisane do użytkownika i widoczne tylko dla niego.

**Role:** flat. Brak ról w MVP. Wszyscy użytkownicy równi. Nie ma admina, dietetyka, pacjenta — dokument w sekcji 7 explicite wyklucza role dietetyk-pacjent.

**Nieautentykowany dostęp:** brak. Każdy dostęp do zasobów użytkownika wymaga zalogowanego użytkownika. Strona publiczna może mieć tylko ekran logowania/rejestracji.

**Przyszłe interfejsy produktu:** gdy wrócą w etapie 2 roadmapy, korzystają z tego samego konta co główna aplikacja — sposób utrzymania sesji to decyzja techniczna po PRD.

---

## Non-Goals

### Funkcjonalne (czego MVP świadomie nie robi)

- **Interfejs konsolowy w MVP** — wraca w etapie 2 roadmapy. Powód: scope-down (Faza 3), żeby MVP zmieścił się w ~4 tygodniach.
- **Tagi przepisów** (`bez cukru`, `wege`, `meal prep`) — wracają w etapie 2 razem z lepszym filtrowaniem. Powód: scope-down.
- **Eksport listy zakupów do pliku** — wraca w etapie 2. Lista w MVP tylko na ekranie. Powód: scope-down.
- **Skaner kodu kreskowego kamerą** — kod wpisuje się ręcznie (zgodne z sekcją 7 dokumentu). Skaner kamerą po MVP.
- **Plan tygodniowy / wielodniowy** — w MVP plan jest pojedynczo-dniowy. Plan 7-dniowy + lista zakupów wielodniowa to Secondary outcome, nie must-have.
- **Społecznościowe feature'y** — feed, lajki, komentarze, publiczne przepisy, obserwowanie użytkowników — wszystko z etapu 4 roadmapy.
- **AI generujące plany dietetyczne** — flagowy kierunek future-scope (etap 5 roadmapy). W MVP użytkownik komponuje plan sam.
- **Spiżarnia / no-waste / daty ważności** — etap 3 roadmapy.
- **Aplikacja mobilna natywna (iOS/Android)** — MVP jest webową aplikacją responsywną (NFR pokrywa mobile-web). Natywna apka jest po MVP.
- **Role administracyjne / dietetyk-pacjent** — flat user model w MVP. Role wracają tylko jeśli pojawi się jasna potrzeba biznesowa.
- **Historia celów / cykle (redukcja vs masa)** — w MVP jeden aktualny zestaw celów. Historyczne zmiany / cykle treningowe po MVP.
- **Edycja istniejących wpisów planu poza zmianą typu posiłku i porcji** — FR-014 pokrywa minimum; bardziej rozbudowana edycja po MVP.

### Niefunkcjonalne (jakości których MVP świadomie nie obiecuje)

- **Compliance ponad podstawowy GDPR** — bez HIPAA, SOC 2, ISO 27001 i podobnych certyfikacji. Powód: niewspółmierne do skali MVP.
- **Offline-first** — aplikacja wymaga połączenia z usługą aplikacyjną. Brak trybu offline w żadnym widoku. Powód: agresywnie podnosi koszt MVP bez wartości proporcjonalnej do scope'u single-user.
- **SLA uptime** — best-effort dostępność; brak deklarowanego procentu (np. 99.9%). MVP nie ma tła do takich zobowiązań.
- **WCAG-AA certyfikacja accessibility** — aplikacja ma być użyteczna z klawiatury i mieć przyzwoite kontrasty, ale bez deklarowania zgodności ze standardem.

---

## Forward: tech-stack

Notatki dla następnego ogniwa łańcucha (`/10x-tech-stack-selector` lub równoważnego). NIE wchodzą do PRD — są kierunkowymi preferencjami autora, zebranymi z dokumentu źródłowego `docs/jadlify-koncepcja-mvp.md` (sekcja 8 "Proponowany stack technologiczny"). Tech-stack-selector może je przyjąć, zmienić lub odrzucić po własnym audycie wymagań:

- **Backend (sugerowany):** ASP.NET Core Web API na .NET 10 LTS, EF Core, modularny monolit.
- **Baza danych (sugerowana):** Supabase PostgreSQL (hostowany).
- **Autoryzacja (sugerowana):** Supabase Auth jako dostawca logowania; logika biznesowa pozostaje w API .NET.
- **Frontend (sugerowany):** React + TypeScript + Vite.
- **CI/CD (sugerowany):** GitHub Actions — build + testy + opcjonalny deployment.
- **CLI (poza MVP, ale w roadmap):** aplikacja konsolowa .NET, docelowo `dotnet tool`.

Forward-looking architectural shape (z sekcji 9 dokumentu): frontend nie wykonuje głównych operacji domenowych na bazie; logika makro / walidacja / lista zakupów żyje w backendzie. Tech-stack-selector ma za zadanie zweryfikować, czy to jest właściwa decyzja przy zebranych FR/NFR.

---

## Forward: technical-roadmap

Notatki dla planowania post-MVP. NIE wchodzą do PRD.

Z sekcji 14 dokumentu źródłowego, w kolejności priorytetowej:

- **Etap 2 — wygoda użytkownika:** zdjęcia posiłków, lepsze filtrowanie, kopiowanie planów między dniami, szablony, eksport CSV/PDF, **CLI**, **tagi przepisów**, plan wielodniowy.
- **Etap 3 — spiżarnia i no-waste:** lista produktów posiadanych, daty ważności, odejmowanie posiadanych od listy zakupów, scoring no-waste, sugestie przepisów wykorzystujących produkty z lodówki.
- **Etap 4 — społeczność:** publiczne przepisy, feed, lajki, komentarze, follow, zapisywanie cudzych przepisów.
- **Etap 5 — AI i agenci:** generowanie planu posiłków na podstawie celu i preferencji, dopasowanie do upodobań, optymalizacja meal prep, automatyczna minimalizacja marnowania, CLI jako narzędzie dla agentów AI, możliwy MCP server.

---

## Open Questions

Pytania, które shaping wyciągnął jako otwarte. `/10x-prd` przeniesie je do `## Open Questions` w PRD i zaznaczy każde jako wymagające rozstrzygnięcia przed implementacją (lub w jej trakcie, jeśli decyzja może być deferred).

1. **Zachowanie zależnych danych po usunięciu produktu/przepisu.** Gdy użytkownik usuwa produkt, który jest składnikiem któregoś z jego przepisów (FR-006), lub przepis, który jest w planie któregoś dnia (FR-010, FR-014), jakie jest oczekiwane zachowanie? Opcje: (a) usunięcie propaguje się na zależne przepisy/wpisy, (b) historyczne wpisy planu zostają zachowane mimo usunięcia źródłowego elementu, (c) usunięcie jest blokowane z komunikatem "produkt używany w X przepisach". Owner: użytkownik. Block: nie jest blokerem MVP, ale decyzja musi zapaść przed pierwszym wdrożeniem.

2. **Jednostki miary produktów (g / ml / szt).** MVP zakłada gramy wszędzie (FR-003), co upraszcza agregację listy zakupów (FR-015). Realne produkty są jednak też w ml (płyny) i szt (jajka, jogurty). Czy MVP zostaje przy gramach z akceptacją niewygody, czy rozróżnia jednostkę produktu i agregację według tej jednostki? Owner: użytkownik. Block: nie, ale wpływa na kształt produktu — lepiej rozstrzygnąć przed pierwszą wersją produkcyjną.

3. **Strategia danych po kodzie kreskowym.** Gdy użytkownik dodaje produkt po kodzie kreskowym, czy wartości odżywcze mają pozostać takie, jak w momencie dodania, czy mają być odświeżane przy późniejszym korzystaniu z produktu? Pierwsze: stabilne, ale dane mogą się zestarzeć. Drugie: świeższe, ale zależne od dostępności danych zewnętrznych i może spowalniać użycie produktu. Owner: tech-stack-selector / implementator. Block: nie.

4. **Granice gramatur w przepisie: per-przepis czy per-porcja.** FR-007/008 wprowadza liczbę porcji do przepisu. Pytanie wynikowe: gdy użytkownik podaje "150g kurczaka" w przepisie na 4 porcje, czy to jest 150g w sumie (37.5g/porcja) czy 150g/porcja (600g w sumie)? Konwencja musi być eksplicytna, najlepiej widoczna dla użytkownika. Owner: implementator (decyzja UX), z rekomendacją gramatury per-cały-przepis (intuicyjne dla meal prep). Block: nie, ale UX musi być jednoznaczne.

5. **Edycja celów dziennych — czy są wersjonowane lub są jednym aktualnym zestawem.** FR-011 mówi "ustawić cele". Decyzja zostaje: jeden aktualny zestaw — historia/wersjonowanie po MVP. Powinno być explicit w UI. Owner: implementator.

---

## Quality cross-check

Wynik kontroli jakości (Faza 7) — wszystkie elementy obecne:

- **Access Control:** present (single-user, flat, jasno opisane).
- **Business Logic:** present (jedno-zdaniowa reguła + 2 fasety: kalibracja kcal/makro vs cele + zagregowana lista zakupów).
- **Project artifacts:** present (`context/foundation/shape-notes.md`).
- **Timeline-cost ack:** present (mvp_weeks: 4, after_hours_only: true, brak hard deadline).
- **Non-Goals:** present (11 funkcjonalnych + 4 niefunkcjonalne, w tym pełen scope-down z Fazy 3).
- **Preserved behavior:** n/a (greenfield).

**Quality check status:** `accepted` — wszystkie wymagane elementy obecne, brak luk do mirror'owania jako warnings do `/10x-prd`.

---
