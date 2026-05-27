# Jadlify — wstępna koncepcja aplikacji

## 1. Nazwa robocza

**Jadlify** — aplikacja do planowania posiłków, komponowania przepisów, liczenia kalorii i makroskładników oraz generowania list zakupów.

Alternatywne nazwy:

- **MakroChef**
- **DietForge**
- **MealPilot**
- **NutriPlan**

Roboczo rekomendowana nazwa: **Jadlify**, ponieważ dobrze pasuje do przyszłej wizji aplikacji: planowanie diety, praca ze spiżarnią/lodówką, no-waste, meal prep oraz integracje z agentami AI.

---

## 2. Krótki opis produktu

Jadlify to webowa aplikacja dla osób, które chcą świadomie planować posiłki, kontrolować kalorie i makroskładniki oraz łatwiej przygotowywać listy zakupów.

Użytkownik może dodawać produkty, pobierać dane produktów po kodzie kreskowym z Open Food Facts, tworzyć przepisy, oznaczać je tagami, układać plan posiłków i sprawdzać, czy jego dieta mieści się w założonych celach kalorycznych oraz makroskładnikowych.

W dłuższej perspektywie aplikacja może rozwinąć się w społecznościowy system wymiany przepisów i planów dietetycznych, a także w narzędzie wspierane przez algorytmy AI do automatycznego komponowania diety.

---

## 3. Główny problem użytkownika

Użytkownik chce planować dietę, ale ręczne liczenie kalorii, makroskładników, gramatur i list zakupów jest czasochłonne oraz podatne na błędy.

Jadlify ma rozwiązać ten problem przez połączenie kilku rzeczy w jednym prostym przepływie:

1. dodanie produktu,
2. stworzenie przepisu,
3. dodanie przepisu do planu posiłków,
4. automatyczne przeliczenie kalorii i makro,
5. wygenerowanie listy zakupów.

---

## 4. Użytkownik docelowy MVP

Pierwszym użytkownikiem aplikacji jest osoba, która:

- samodzielnie planuje posiłki,
- liczy kalorie lub makroskładniki,
- przygotowuje jedzenie na kilka dni,
- chce ograniczyć ręczne przepisywanie danych produktów,
- chce szybko sprawdzić, czy plan dnia lub tygodnia zgadza się z jej celami.

MVP nie jest jeszcze aplikacją dla dietetyków, trenerów, rodzin ani społeczności. Te kierunki mogą pojawić się później.

---

## 5. Główna logika biznesowa

Aplikacja przelicza wartości odżywcze przepisów i planów posiłków na podstawie produktów oraz gramatur, a następnie porównuje wynik z celami użytkownika.

Przykład:

```text
Produkt ma wartości odżywcze na 100 g.
Użytkownik dodaje 80 g produktu do przepisu.
Aplikacja przelicza kcal, białko, tłuszcze i węglowodany a także błonnik itd dla tej ilości.
Następnie sumuje składniki przepisu, przepisy w planie dnia i cały okres planowania.
```

Dodatkowa logika w MVP:

- agregowanie składników do listy zakupów,
- oznaczanie przepisów tagami,
- walidacja kompletności danych produktu,
- obsługa produktu znalezionego lub nieznalezionego w Open Food Facts.

---

## 6. Zakres MVP

### 6.1. Funkcje obowiązkowe

1. **Logowanie użytkownika**
   - użytkownik posiada własne konto,
   - dane produktów, przepisów i planów są przypisane do użytkownika.

2. **Produkty**
   - dodawanie produktu ręcznie,
   - edycja produktu,
   - usuwanie produktu,
   - lista produktów użytkownika,
   - pola: nazwa, kod kreskowy, kcal/100 g, białko/100 g, tłuszcze/100 g, węglowodany/100 g.

3. **Integracja z Open Food Facts**
   - użytkownik wpisuje kod kreskowy,
   - aplikacja odpytuje Open Food Facts,
   - jeżeli produkt istnieje, aplikacja wypełnia formularz produktu danymi z API,
   - użytkownik może poprawić dane przed zapisem,
   - jeżeli produkt nie istnieje lub dane są niepełne, użytkownik może uzupełnić formularz ręcznie.

4. **Przepisy**
   - tworzenie przepisu z produktów,
   - określenie gramatur składników,
   - automatyczne liczenie makro przepisu,
   - tagi, np. `bez cukru`, `bez glutenu`, `wege`, `meal prep`, `no waste`.

5. **Plan posiłków**
   - dodanie przepisu do dnia,
   - wybór typu posiłku, np. śniadanie, obiad, kolacja, przekąska,
   - podsumowanie kalorii i makroskładników dla dnia.

6. **Cele użytkownika**
   - ustawienie dziennego celu kcal,
   - ustawienie celu białka, tłuszczów i węglowodanów,
   - pokazanie różnicy między planem a celem.

7. **Lista zakupów**
   - wygenerowanie listy zakupów na podstawie planu posiłków,
   - agregowanie tych samych produktów z wielu przepisów,
   - eksport listy do prostego formatu tekstowego lub CSV.

8. **CLI**
   - pobieranie planu na wybrany dzień,
   - generowanie listy zakupów,
   - opcjonalnie dodanie produktu po kodzie kreskowym.

---

## 7. Poza zakresem MVP

Na potrzeby kursu nie wchodzą do pierwszej wersji:

- feed społecznościowy,
- lajki i komentarze,
- obserwowanie użytkowników,
- publiczne profile,
- aplikacja mobilna,
- skanowanie kodu kreskowego kamerą,
- pełne AI do automatycznego układania diety,
- zaawansowana optymalizacja no-waste,
- role dietetyk-pacjent,
- płatności,
- rozbudowane raporty i wykresy.

Te funkcje powinny zostać w roadmapie, ale nie powinny blokować pierwszego działającego przepływu.

---

## 8. Proponowany stack technologiczny

### Backend

- **ASP.NET Core Web API**
- **.NET 10 LTS**
- **EF Core**
- architektura: modularny monolit

### Baza danych

- **Supabase PostgreSQL**

Supabase jest dobrym wyborem na MVP, ponieważ zapewnia hostowanego PostgreSQL-a, panel administracyjny i dodatkowe usługi, bez potrzeby samodzielnego utrzymywania bazy na VPS-ie.

### Autoryzacja

- **Supabase Auth** albo własne auth w ASP.NET Core

Rekomendowany wariant: Supabase Auth jako dostawca logowania, ale główna logika biznesowa nadal w backendzie .NET.

### Frontend

- **React**
- **TypeScript**
- **Vite**

### CLI

- aplikacja konsolowa w .NET,
- docelowo możliwa do spakowania jako `dotnet tool`.

### CI/CD

- GitHub Actions,
- automatyczny build,
- uruchamianie testów,
- opcjonalny deployment backendu i frontendu.

---

## 9. Proponowana architektura

```text
React Web App
    ↓
Supabase Auth
    ↓
ASP.NET Core Web API
    ↓
Supabase PostgreSQL
    ↓
CLI korzystające z tego samego API
```

Frontend nie powinien bezpośrednio wykonywać głównych operacji domenowych na bazie. Powinien korzystać z API .NET, ponieważ tam znajduje się logika biznesowa: liczenie makro, walidacja, generowanie list zakupów i obsługa reguł aplikacji.

---

## 10. Integracja z Open Food Facts w MVP

Integracja powinna być prosta i odporna na niepełne dane.

### Przepływ użytkownika

1. Użytkownik wybiera opcję „Dodaj produkt po kodzie kreskowym”.
2. Wpisuje kod kreskowy ręcznie.
3. Backend odpytuje Open Food Facts.
4. Jeżeli produkt zostanie znaleziony, aplikacja mapuje dane do formularza produktu.
5. Użytkownik widzi formularz i może poprawić dane.
6. Użytkownik zapisuje produkt w swojej bazie.

### Ważne założenie

Dane z Open Food Facts powinny być traktowane jako sugestia, a nie jako nieomylne źródło prawdy. Produkt może mieć niepełne wartości odżywcze, inny wariant regionalny albo brak danych dla części makroskładników.

### Minimalne mapowanie danych

```text
Open Food Facts → Produkt w aplikacji

product_name          → name
code                  → barcode
nutriments.energy-kcal_100g → caloriesPer100g
nutriments.proteins_100g    → proteinPer100g
nutriments.fat_100g         → fatPer100g
nutriments.carbohydrates_100g → carbsPer100g
brands                → brandName, opcjonalnie
image_url             → imageUrl, opcjonalnie
```

---

## 11. Przykładowy główny przepływ demo

1. Użytkownik loguje się do aplikacji.
2. Wpisuje kod kreskowy produktu.
3. Aplikacja pobiera dane z Open Food Facts.
4. Użytkownik zapisuje produkt.
5. Użytkownik tworzy przepis z tego produktu i innych składników.
6. Aplikacja liczy makro przepisu.
7. Użytkownik dodaje przepis do planu dnia.
8. Aplikacja pokazuje podsumowanie dnia i różnicę względem celu.
9. Użytkownik generuje listę zakupów.
10. Użytkownik pobiera listę zakupów przez CLI.

---

## 12. Minimalny model danych

```text
User
Product
Recipe
RecipeIngredient
MealPlan
MealPlanEntry
DailyNutritionTarget
Tag
RecipeTag
ShoppingListSnapshot, opcjonalnie
```

Najważniejsze relacje:

```text
User 1 - N Product
User 1 - N Recipe
Recipe 1 - N RecipeIngredient
Product 1 - N RecipeIngredient
User 1 - N MealPlan
MealPlan 1 - N MealPlanEntry
Recipe 1 - N MealPlanEntry
Recipe N - N Tag
```

---

## 13. Testy w MVP

Minimalny zestaw testów:

1. **Test jednostkowy kalkulacji makro przepisu**
   - sprawdza, czy aplikacja poprawnie przelicza wartości odżywcze na podstawie gramatur.

2. **Test integracyjny API dla głównego przepływu**
   - dodanie produktu,
   - stworzenie przepisu,
   - dodanie przepisu do planu dnia,
   - sprawdzenie podsumowania makro.

3. **Test integracji z Open Food Facts**
   - mock odpowiedzi API,
   - sprawdzenie mapowania danych z Open Food Facts do modelu produktu.

4. **Test E2E, opcjonalnie**
   - użytkownik loguje się,
   - dodaje produkt,
   - tworzy przepis,
   - widzi poprawne podsumowanie.

---

## 14. Roadmapa po MVP

### Etap 1 — MVP kursowe

- logowanie,
- produkty,
- import produktu po kodzie kreskowym z Open Food Facts,
- przepisy,
- tagi,
- plan posiłków,
- cele dzienne,
- kalkulacja makro,
- lista zakupów,
- podstawowe CLI,
- testy i CI/CD.

### Etap 2 — wygoda użytkownika

- zdjęcia posiłków,
- lepsze filtrowanie przepisów,
- kopiowanie planów między dniami,
- szablony posiłków,
- eksport listy zakupów do PDF/CSV,
- responsywny widok mobilny.

### Etap 3 — spiżarnia i no-waste

- lista produktów posiadanych w domu,
- daty ważności,
- odejmowanie posiadanych produktów od listy zakupów,
- prosty scoring no-waste,
- sugestie przepisów wykorzystujących produkty z lodówki.

### Etap 4 — społeczność

- publiczne przepisy,
- feed,
- lajki,
- komentarze,
- zapisywanie cudzych przepisów,
- obserwowanie użytkowników.

### Etap 5 — AI i agenci

- generowanie planu posiłków na podstawie celu,
- dopasowanie planu do preferencji użytkownika,
- optymalizacja meal prep na kilka dni,
- automatyczna minimalizacja marnowania produktów,
- CLI jako narzędzie dla agentów AI,
- możliwy MCP server dla integracji z asystentami AI.

---

## 15. Największe ryzyka

1. **Za duży zakres MVP**
   - rozwiązanie: społeczność, AI i mobile zostają poza pierwszą wersją.

2. **Niepełne dane z Open Food Facts**
   - rozwiązanie: zawsze pozwolić użytkownikowi poprawić dane przed zapisem.

3. **Zbyt wczesne komplikowanie auth i RLS**
   - rozwiązanie: logowanie ma działać prosto, a logika domenowa ma być w API .NET.

4. **Rozlanie logiki między frontend i backend**
   - rozwiązanie: frontend pokazuje dane, backend liczy i waliduje.

5. **CLI zbyt ambitne na start**
   - rozwiązanie: w MVP CLI obsługuje tylko kilka najważniejszych komend.

---

## 16. Definicja sukcesu MVP

MVP można uznać za gotowe, jeżeli użytkownik może przejść cały przepływ:

```text
logowanie
→ dodanie produktu ręcznie lub po kodzie kreskowym
→ stworzenie przepisu
→ dodanie przepisu do planu dnia
→ zobaczenie podsumowania kcal i makro
→ wygenerowanie listy zakupów
→ pobranie listy zakupów przez CLI
```

To jest wystarczająco małe, żeby dowieźć projekt kursowy, ale jednocześnie wystarczająco konkretne, żeby pokazać realną logikę biznesową i potencjał rozwoju produktu.
