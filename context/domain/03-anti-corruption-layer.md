---
title: Anti-Corruption Layer (ACL) Plan — Jadlify
created: 2026-08-05
type: refactor-plan
---

# Plan Projektowy Anti-Corruption Layer (ACL) — Jadlify

Dokument przedstawia szczegółową analizę, identyfikację, klasyfikację, diagnozę oraz projekt izolacji i refaktoryzacji przeciekających zależności zewnętrznych w systemie **Jadlify** z wykorzystaniem wzorca **Anti-Corruption Layer (ACL)** w architekturze Domain-Driven Design (DDD).

---

## KROK 0 — Odkrycie Kontekstu (Context & Architectural Intent)

### 1. Deklaracje Bazowe z Dokumentów Projektowych
Analiza została przeprowadzona na podstawie dokumentów architektury i PRD:
- **Deklaracja Wymienialności Tożsamości**: [prd.md:178](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L178) (*"Model: każdy użytkownik ma własne konto z indywidualnym logowaniem; wybór dostawcy i mechanizmu logowania jest decyzją techniczną po PRD."*). Dokument produktowy jawnie deklaruje, że dostawca tożsamości jest szczegółem technicznym, a domena i interfejs użytkownika mają być od niego odseparowane.
- **Intencja Stosu Technologicznego**: [tech-stack.md:31](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/tech-stack.md#L31) (*"The React frontend may use the Supabase client only for authentication/session management. Product, recipe, goal, meal-plan, macro-summary, and shopping-list behavior must go through the ASP.NET Core API."*). Intencją architektoniczną jest ograniczenie użycia dostawcy Auth wyłącznie do zarządzania sesją, bez infekowania logiki aplikacyjnej.
- **Izolacja Danych i Uwierzytelnianie API**: [tech-stack.md:33](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/tech-stack.md#L33) (*"The ASP.NET Core API validates Supabase-issued JWT bearer tokens, treats the token sub claim as the stable application user id, and enforces per-user authorization in the application/data-access layer."*).

### 2. Zależności Zewnętrzne i Manifesty Pakietów
Na podstawie inspekcji plików projektowych solucji zmapowano wszystkie zależności zewnętrzne zdefiniowane w projektach:
1. **Frontend SPA (`src/Jadlify.Web`)**:
   - `@supabase/supabase-js` (`^2.106.2`) zadeklarowany w [package.json:18](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Web/package.json#L18).
   - `@tanstack/react-query` (`^5.100.14`) w [package.json:19](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Web/package.json#L19).
2. **Backend API (`src/Jadlify.API`)**:
   - `Microsoft.AspNetCore.Authentication.JwtBearer` (`10.0.7`) w [Jadlify.API.csproj:14](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.API/Jadlify.API.csproj#L14).
3. **Warstwa Persystencji (`src/Jadlify.Infrastructure`)**:
   - `Npgsql.EntityFrameworkCore.PostgreSQL` (`10.0.2`) w [Jadlify.Infrastructure.csproj:15](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Infrastructure/Jadlify.Infrastructure.csproj#L15).
   - Zewnętrzne REST API Open Food Facts (integracja HTTP) w [OpenFoodFactsBarcodeLookup.cs:19](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Infrastructure/OpenFoodFacts/OpenFoodFactsBarcodeLookup.cs#L19).
4. **Warstwa Aplikacyjna (`src/Jadlify.Application`)**:
   - `FluentValidation` (`12.1.1`) w [Jadlify.Application.csproj:8](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Application/Jadlify.Application.csproj#L8).
5. **Warstwa Domenowa (`src/Jadlify.Domain`)**:
   - **Zero zależności zewnętrznych** [Jadlify.Domain.csproj:1-12](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Jadlify.Domain.csproj#L1-L12).

---

## KROK 1 — Identyfikacja Przeciekających Zależności

Wykryto i zestawiono zależności zewnętrzne przenikające przez granice architektoniczne:

### 1. Przeciek #1 (KRYTYCZNY): SDK Autentykacji Supabase (`@supabase/supabase-js`)
SDK Supabase infekuje całą warstwę prezentacji (React SPA), stan kontekstu aplikacji, handlery błędów, klienta HTTP API oraz zestaw testów jednostkowych i integracyjnych.

**Wszystkie pliki (plik:linia) znające dziś zależność `@supabase/supabase-js` / Supabase Auth:**
- `src/Jadlify.Web/package.json:18` (Deklaracja pakietu npm)
- `src/Jadlify.Web/src/lib/supabase.ts:1-17` (Inicjalizacja i eksport pojedynczej instancji SDK)
- `src/Jadlify.Web/src/auth/SessionContext.ts:2,7` (Import typu `Session` z `@supabase/supabase-js` bezpośrednio do interfejsu stanu kontekstu React)
- `src/Jadlify.Web/src/auth/SessionProvider.tsx:3,4,18,28` (Wywoływanie `supabase.auth.getSession()` oraz `supabase.auth.onAuthStateChange()`)
- `src/Jadlify.Web/src/auth/authErrors.ts:2,10-32` (Konwersja błędów SDK Supabase z dopasowywaniem napisów/kodów błędów Supabase)
- `src/Jadlify.Web/src/auth/useSession.ts:4` (Przekazywanie stanu kontekstu zawierającego typ `Session` z Supabase)
- `src/Jadlify.Web/src/api/apiClient.ts:1,7` (Bezpośrednie wywołanie `supabase.auth.getSession()` w celu wyciągnięcia `access_token`)
- `src/Jadlify.Web/src/routes/LoginPage.tsx:4,78,97` (Bezpośrednie wołanie `supabase.auth.signUp()` i `supabase.auth.signInWithPassword()` wewnątrz formularza UI)
- `src/Jadlify.Web/src/layout/AccountMenu.tsx:2,15,60` (Bezpośrednie wołanie `supabase.auth.signOut()` wewnątrz komponentu nagłówka)
- `src/Jadlify.Web/src/App.test.tsx:8-9` (Mockowanie klienckiego SDK `supabase` w teście)
- `src/Jadlify.Web/src/auth/RequireAuth.test.tsx:4` (Import typu `Session` z Supabase na potrzeby mockowania)
- `src/Jadlify.Web/src/auth/SessionProvider.test.tsx:3,12-13,58` (Mockowanie struktur SDK Supabase)
- `src/Jadlify.Web/src/layout/AccountMenu.test.tsx:4,8,10-11,14,84` (Mockowanie funkcji `signOut` z `supabase.auth`)
- `src/Jadlify.Web/src/routes/LoginPage.test.tsx:4,10-12` (Mockowanie metod `signInWithPassword` i `signUp` z SDK `supabase.auth`)
- `src/Jadlify.API/Authentication/SupabaseJwtOptions.cs:3,5` (Opcje konfiguracyjne specyficzne dla dostawcy w API)
- `src/Jadlify.API/Authentication/HttpContextCurrentUser.cs:10,14` (Odczyt `ClaimsPrincipal` i opcji JWT)
- `src/Jadlify.API/Program.cs:28,32,45,47` (Rejestracja i konfiguracja walidatora tokenów JWT od Supabase)

### 2. Przeciek #2 (POPRAWNIE ZAINKSULOWANA): Zewnętrzne API Open Food Facts
- **Pliki**: `src/Jadlify.Infrastructure/OpenFoodFacts/OpenFoodFactsBarcodeLookup.cs:19`, `OpenFoodFactsResponse.cs:1-60`, `OpenFoodFactsOptions.cs:1-20`.
- **Ocena**: Brak przecieku. Integracja jest zamknięta w warstwie `Infrastructure` i realizuje interfejs aplikacyjny `IBarcodeProductLookup` ([IBarcodeProductLookup.cs:11](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Application/Products/IBarcodeProductLookup.cs#L11)). Żadne typy JSON ani modele odpowiedzi Open Food Facts nie przeciekają do warstwy `Application` ani `Domain`.

### 3. Przeciek #3 (POPRAWNIE ZAINKSULOWANA): EF Core i Npgsql PostgreSQL
- **Pliki**: `src/Jadlify.Infrastructure/Persistence/*`.
- **Ocena**: Brak przecieku. EF Core znajduje się wyłącznie w warstwie persystencji. Encje domenowe w `src/Jadlify.Domain` nie posiadają adnotacji ORM ani zależności do `Microsoft.EntityFrameworkCore`.

---

## KROK 2 — Klasyfikacja i Wybór Przecieku #1

### Ocena Zależności na Osiach Ryzyka i Kosztu:

| Zależność Zewnętrzna | (a) Liczba Warstw / Plików Dotkniętych | (b) Ryzyko / Koszt Wymiany Dzisiaj | (c) Rozjazd Intencja z Dokumentów vs Kod | Weryfikacja i Verdict |
| :--- | :--- | :--- | :--- | :--- |
| **Supabase Auth SDK (`@supabase/supabase-js`)** | **18 plików** (Komponenty UI, Widoki, Kontekst React, Klient API, Handlery błędów, Testy) | **BARDZO WYSOKI**: Wymiana dostawcy Auth (np. na Auth0, Keycloak, Firebase czy ASP.NET Core Identity) wymagałaby zmian w 14+ plikach widoków i testów SPA. | **EKSTREMALNY**: PRD ([prd.md:178](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L178)) wprost określa wybór dostawcy jako szczegół techniczny po PRD. Kod produkcyjny jest zaś ściśle związany z API i typami Supabase. | **WYBRANY PRZECIEK #1** |
| **Open Food Facts API** | 3 pliki (wyłącznie w `Jadlify.Infrastructure`) | **NISKI**: Ukryty za portem `IBarcodeProductLookup`. Wymiana na USDA/EDAMAM dotyka 1 pliku. | **BRAK ROZJAZDU**: Zgodne z architekturą portów i adapterów. | Poprawnie odseparowany |
| **EF Core / Npgsql** | 12 plików (wyłącznie w `Jadlify.Infrastructure`) | **UMIARKOWANY**: Dostępny przez wzorzec Repozytorium (`IProductRepository`, `IMealPlanRepository`). | **BRAK ROZJAZDU**: Persystencja odizolowana od domeny. | Poprawnie odseparowany |

### Uzasadnienie Wyboru Przecieku #1:
Zależność **Supabase Auth SDK (`@supabase/supabase-js`)** jest najgorszym i najbardziej rozległym przeciekiem w systemie. Choć dokumentacja architektoniczna zadeklarowała odseparowanie mechanizmu autentykacji, w praktyce typy biblioteki (`Session`, `User`, `AuthError`) oraz metody SDK (`signInWithPassword`, `signUp`, `signOut`, `getSession`) przeniknęły bezpośrednio do komponentów prezentacji React (`LoginPage.tsx`, `AccountMenu.tsx`), do globalnego stanu aplikacji (`SessionContext.ts`), do warstwy komunikacji HTTP (`apiClient.ts`) oraz do całego zestawu testów jednostkowych UI.

---

## KROK 3 — Diagnoza Przecieku

### 1. Bezpośrednie Wołanie SDK w Komponentach Prezentacji UI
Przegląd kodu wskazuje, że widoki i komponenty interfejsu użytkownika bezpośrednio wykonują operacje na kliencie Supabase zamiast korzystać z abskrakcji portu autentykacji:

> [!WARNING]
> Cytat z `LoginPage.tsx` — Bezpośrednie wywołanie SDK w formularzu logowania:
> [LoginPage.tsx:78-81](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Web/src/routes/LoginPage.tsx#L78-L81):
> ```typescript
> const { data, error: signUpError } = await supabase.auth.signUp({
>   email: trimmedEmail,
>   password,
> })
> ```
> [LoginPage.tsx:97-100](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Web/src/routes/LoginPage.tsx#L97-L100):
> ```typescript
> const { error: signInError } = await supabase.auth.signInWithPassword({
>   email: trimmedEmail,
>   password,
> })
> ```

> [!WARNING]
> Cytat z `AccountMenu.tsx` — Bezpośrednie wywołanie SDK w nagłówku aplikacji:
> [AccountMenu.tsx:60](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Web/src/layout/AccountMenu.tsx#L60):
> ```typescript
> await supabase.auth.signOut()
> ```

### 2. Infekcja Typami Biblioteki w Kontraktach i Stanie Aplikacji
Typ `Session` dostarczany przez `@supabase/supabase-js` przecieka do interfejsu stanu kontekstu aplikacji:

> [!CAUTION]
> Cytat z `SessionContext.ts` — Przeciek typu dostawcy do stanu kontekstu React:
> [SessionContext.ts:2](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Web/src/auth/SessionContext.ts#L2):
> ```typescript
> import type { Session } from '@supabase/supabase-js'
> ```
> [SessionContext.ts:7](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Web/src/auth/SessionContext.ts#L7):
> ```typescript
> session: Session | null
> ```

### 3. Uzależnienie Klienta HTTP od Konkretnego SDK
Klient komunikacji z backendowym API ASP.NET Core wywołuje bezpośrednio metodę `supabase.auth.getSession()`:

> [!WARNING]
> Cytat z `apiClient.ts` — Przeciek SDK do klienta API:
> [apiClient.ts:7](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Web/src/api/apiClient.ts#L7):
> ```typescript
> const { data } = await supabase.auth.getSession()
> return data.session?.access_token ?? null
> ```

### 4. Sprzeczność z Deklaracją w PRD
PRD wprost zakłada neutralność autentykacji:
> [!IMPORTANT]
> Cytat z `prd.md` — Deklaracja wymienialności:
> [prd.md:178](file:///c:/Users/wikla/source/repos/Jadlify/context/foundation/prd.md#L178):
> *"Model: każdy użytkownik ma własne konto z indywidualnym logowaniem; wybór dostawcy i mechanizmu logowania jest decyzją techniczną po PRD."*

Obecny kod łamie tę zasadę: zmiana dostawcy na Auth0 lub własne tokeny JWT wymusiłaby zmianę struktury stanu w `SessionContext.ts`, przebudowanie logiki logowania i wylogowania we wszystkich komponentach UI, przepisanie wywołań w `apiClient.ts` oraz modyfikację wszystkich zaślepek w testach frontendowych.

---

## KROK 4 — Projekt Anti-Corruption Layer (ACL)

Projekt ACL wprowadzający ścisłą izolację tożsamości i sesji użytkownika od konkretnego dostawcy (Supabase Auth).

### 1. Obiekty Domenowe / Aplikacyjne (Value Objects & Entities)
Zaprojektowano czyste typy domeny/aplikacji reprezentujące tożsamość, sesję oraz poświadczenia, niezależne od jakichkolwiek struktur zewnętrznych SDK.

#### A. Value Object: `AuthUser`
```typescript
/**
 * Agnostyczna tożsamość zalogowanego użytkownika w warstwie prezentacji/aplikacji.
 */
export interface AuthUser {
  readonly id: string
  readonly email: string
}
```

#### B. Value Object: `UserSession`
```typescript
/**
 * Agnostyczna sesja użytkownika stanowiąca jedyne źródło prawdy dla aplikacji SPA.
 */
export interface UserSession {
  readonly accessToken: string
  readonly user: AuthUser
  readonly expiresAt?: number
}
```

#### C. Value Object: `AuthCredentials`
```typescript
/**
 * Poświadczenia logowania / rejestracji.
 */
export interface AuthCredentials {
  readonly email: string
  readonly password: string
}
```

#### D. Value Object: `AuthOutcome` (Wynik Operacji Auth)
```typescript
export type AuthFailureReason =
  | 'InvalidCredentials'
  | 'UserAlreadyExists'
  | 'WeakPassword'
  | 'NetworkError'
  | 'Unknown'

export type AuthOutcome =
  | { readonly success: true; readonly session: UserSession }
  | { readonly success: false; readonly reason: AuthFailureReason; readonly message: string }
```

### 2. Wąski Port Domenowy / Aplikacyjny (`IAuthService`)
Zdefiniowano wąski interfejs portu autentykacji, z którego korzystać będzie cała aplikacja React SPA.

```typescript
/**
 * Port autentykacji w warstwie aplikacji/UI.
 * Żaden komponent UI ani klient HTTP nie zna konkretnego dostawcy Auth.
 */
export interface IAuthService {
  /** Pobiera aktualną aktywną sesję użytkownika lub null gdy niezalogowany. */
  getSession(): Promise<UserSession | null>

  /** Loguje użytkownika na podstawie poświadczeń. */
  signIn(credentials: AuthCredentials): Promise<AuthOutcome>

  /** Rejestruje nowego użytkownika. */
  signUp(credentials: AuthCredentials): Promise<AuthOutcome>

  /** Wylogowuje użytkownika i unieważnia sesję. */
  signOut(): Promise<void>

  /** Subskrybuje zmiany stanu sesji (np. wygaśnięcie, odświeżenie tokena). */
  onSessionChange(callback: (session: UserSession | null) => void): () => void
}
```

### 3. Adapter Implementujący Port przez Supabase SDK (`SupabaseAuthAdapter`)
Adapter jest **JEDYNYM** miejscem w całej aplikacji frontendowej, które importuje `@supabase/supabase-js` i posiada wiedzę o strukturze `Session`, `User` oraz `AuthError`.

```typescript
import { createClient } from '@supabase/supabase-js'
import type { SupabaseClient, Session, AuthError } from '@supabase/supabase-js'
import type {
  IAuthService,
  UserSession,
  AuthCredentials,
  AuthOutcome,
  AuthFailureReason
} from './IAuthService'

export class SupabaseAuthAdapter implements IAuthService {
  private readonly client: SupabaseClient

  constructor(supabaseUrl: string, supabaseAnonKey: string) {
    this.client = createClient(supabaseUrl, supabaseAnonKey)
  }

  public async getSession(): Promise<UserSession | null> {
    const { data } = await this.client.auth.getSession()
    return this.mapSession(data.session)
  }

  public async signIn(credentials: AuthCredentials): Promise<AuthOutcome> {
    try {
      const { data, error } = await this.client.auth.signInWithPassword({
        email: credentials.email,
        password: credentials.password,
      })

      if (error) {
        return this.mapError(error)
      }

      if (!data.session) {
        return {
          success: false,
          reason: 'Unknown',
          message: 'Brak aktywnej sesji po zalogowaniu.',
        }
      }

      return { success: true, session: this.mapSession(data.session)! }
    } catch (caught) {
      return this.mapError(caught)
    }
  }

  public async signUp(credentials: AuthCredentials): Promise<AuthOutcome> {
    try {
      const { data, error } = await this.client.auth.signUp({
        email: credentials.email,
        password: credentials.password,
      })

      if (error) {
        return this.mapError(error)
      }

      if (!data.session) {
        return {
          success: false,
          reason: 'Unknown',
          message: 'Konto utworzone. Sprawdź skrzynkę e-mail, aby potwierdzić rejestrację.',
        }
      }

      return { success: true, session: this.mapSession(data.session)! }
    } catch (caught) {
      return this.mapError(caught)
    }
  }

  public async signOut(): Promise<void> {
    await this.client.auth.signOut()
  }

  public onSessionChange(callback: (session: UserSession | null) => void): () => void {
    const { data: { subscription } } = this.client.auth.onAuthStateChange((_event, session) => {
      callback(this.mapSession(session))
    })

    return () => subscription.unsubscribe()
  }

  private mapSession(session: Session | null): UserSession | null {
    if (!session || !session.user || !session.user.email) {
      return null
    }

    return {
      accessToken: session.access_token,
      expiresAt: session.expires_at,
      user: {
        id: session.user.id,
        email: session.user.email,
      },
    }
  }

  private mapError(error: unknown): AuthOutcome {
    const code = this.readField(error, 'code')
    const message = this.readField(error, 'message').toLowerCase()

    let reason: AuthFailureReason = 'Unknown'
    let userMessage = 'Coś poszło nie tak. Spróbuj ponownie.'

    if (code === 'invalid_credentials' || message.includes('invalid login credentials')) {
      reason = 'InvalidCredentials'
      userMessage = 'Nieprawidłowy e-mail lub hasło.'
    } else if (
      code === 'user_already_exists' ||
      code === 'email_exists' ||
      message.includes('user already registered')
    ) {
      reason = 'UserAlreadyExists'
      userMessage = 'Konto z tym adresem e-mail już istnieje. Spróbuj się zalogować.'
    } else if (code === 'weak_password' || message.includes('password should be at least')) {
      reason = 'WeakPassword'
      userMessage = 'Hasło musi mieć co najmniej 6 znaków.'
    }

    return { success: false, reason, message: userMessage }
  }

  private readField(error: unknown, key: 'code' | 'message'): string {
    if (typeof error === 'object' && error !== null && key in error) {
      const value = (error as Record<string, unknown>)[key]
      return typeof value === 'string' ? value : ''
    }
    return ''
  }
}
```

---

## KROK 5 — Dowód Izolacji oraz Porównanie Before / After

### 1. Dowód Izolacji: Koszt Wymiany Dostawcy Auth (np. na Auth0 / Keycloak / Custom JWT)
Gdyby podjęto decyzję o wymianie Supabase Auth na innego dostawcę tożsamości:
- **Przed Refaktoryzacją (Dziś)**: Modyfikacja **14+ plików** (`LoginPage.tsx`, `AccountMenu.tsx`, `SessionContext.ts`, `SessionProvider.tsx`, `authErrors.ts`, `apiClient.ts`, `supabase.ts` oraz 7 plików testowych).
- **Po Refaktoryzacją (z ACL)**: Modyfikacja **WYŁĄCZNIE 1 PLIKU** — utworzenie nowego adaptera `Auth0AuthAdapter.ts` implementującego interfejs `IAuthService`. Komponenty UI, widoki, routing, pobieranie tokena w `apiClient.ts` oraz wszystkie testy UI pozostają w 100% nietknięte.

### 2. Porównanie Before / After dla Zduplikowanych i Przeciekających Miejsc

#### A. Rejestracja i Logowanie w `LoginPage.tsx`
- **BEFORE** ([LoginPage.tsx:78-105](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Web/src/routes/LoginPage.tsx#L78-L105)):
  ```typescript
  // PRZED REFAKTOREM: Bezpośrednia zależność od SDK Supabase w UI
  const { data, error: signUpError } = await supabase.auth.signUp({
    email: trimmedEmail,
    password,
  })
  if (signUpError) {
    setError(toAuthMessage(signUpError))
    return
  }
  ```
- **AFTER** (Z wykorzystaniem portu ACL):
  ```typescript
  // PO REFAKTORZE: Wywołanie wąskiego portu IAuthService
  const outcome = isSignup
    ? await authService.signUp({ email: trimmedEmail, password })
    : await authService.signIn({ email: trimmedEmail, password })

  if (!outcome.success) {
    setError(outcome.message)
    return
  }
  navigate('/', { replace: true })
  ```

#### B. Stan Kontekstu w `SessionContext.ts`
- **BEFORE** ([SessionContext.ts:2-7](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Web/src/auth/SessionContext.ts#L2-L7)):
  ```typescript
  // PRZED REFAKTOREM: Typ z biblioteki zewnętrznej w stanie kontekstu
  import type { Session } from '@supabase/supabase-js'
  export interface SessionState {
    session: Session | null
    isLoading: boolean
  }
  ```
- **AFTER** (Czysty obiekt wartości domeny/aplikacji):
  ```typescript
  // PO REFAKTORZE: Domenowy Value Object UserSession
  import type { UserSession } from './IAuthService'
  export interface SessionState {
    session: UserSession | null
    isLoading: boolean
  }
  ```

#### C. Wylogowanie w `AccountMenu.tsx`
- **BEFORE** ([AccountMenu.tsx:60](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Web/src/layout/AccountMenu.tsx#L60)):
  ```typescript
  // PRZED REFAKTOREM: Wywołanie SDK Supabase z poziomu nagłówka
  await supabase.auth.signOut()
  ```
- **AFTER** (Abstrakcja portu):
  ```typescript
  // PO REFAKTORZE: Czyste wywołanie portu
  await authService.signOut()
  ```

#### D. Pobieranie Tokena w `apiClient.ts`
- **BEFORE** ([apiClient.ts:7](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Web/src/api/apiClient.ts#L7)):
  ```typescript
  // PRZED REFAKTOREM: Odczyt tokena z SDK Supabase
  const { data } = await supabase.auth.getSession()
  return data.session?.access_token ?? null
  ```
- **AFTER** (Pobranie tokena z portu `IAuthService`):
  ```typescript
  // PO REFAKTORZE: Odczyt tokena z agnostycznej sesji ACL
  const session = await authService.getSession()
  return session?.accessToken ?? null
  ```

---

## KROK 6 — Weryfikacja i Plan Wdrożenia

### 1. Kryterium Sukcesu Weryfikacji
- **Kryterium główne**: Uruchomienie komendy grep / ripgrep po `@supabase/supabase-js` w katalogu `src/Jadlify.Web/src/` zwraca wynik **WYŁĄCZNIE w plikach adaptera ACL**:
  - `src/Jadlify.Web/src/auth/adapters/SupabaseAuthAdapter.ts`
  - `src/Jadlify.Web/src/auth/adapters/supabaseClient.ts`

### 2. Tabela Porównawcza Świadomości Zależności (Przed vs Po Refaktorze)

| Plik w Systemie | Stan Dzisiaj (Przed Refaktorem) | Stan Targetowy (Po Refaktoryzacji ACL) |
| :--- | :--- | :--- |
| `src/Jadlify.Web/src/lib/supabase.ts` | Importuje i inicjalizuje `@supabase/supabase-js` | **USUNIĘTY** / Zastąpiony przez `SupabaseAuthAdapter.ts` |
| `src/Jadlify.Web/src/auth/SessionContext.ts` | Zna `Session` z `@supabase/supabase-js` | **NIE ZNA** (Używa agnostycznego `UserSession`) |
| `src/Jadlify.Web/src/auth/SessionProvider.tsx` | Zna i wywołuje `supabase.auth` | **NIE ZNA** (Używa portu `IAuthService`) |
| `src/Jadlify.Web/src/auth/authErrors.ts` | Zna opisy i kody błędów Supabase | **USUNIĘTY** / Logika zaimplementowana wewnątrz adaptera |
| `src/Jadlify.Web/src/routes/LoginPage.tsx` | Zna `supabase.auth.signInWithPassword` & `signUp` | **NIE ZNA** (Używa wyłącznie `IAuthService`) |
| `src/Jadlify.Web/src/layout/AccountMenu.tsx` | Zna `supabase.auth.signOut` | **NIE ZNA** (Używa wyłącznie `IAuthService`) |
| `src/Jadlify.Web/src/api/apiClient.ts` | Zna `supabase.auth.getSession()` | **NIE ZNA** (Używa wyłącznie `IAuthService.getSession()`) |
| `src/Jadlify.Web/src/*.test.tsx` (Wszystkie testy) | Mockują bezpośrednio klienta `supabase.auth` | **NIE ZNAJĄ** (Mockują prosty interfejs `IAuthService`) |
| `src/Jadlify.Web/src/auth/adapters/SupabaseAuthAdapter.ts` | **NOWY PLIK** | **ZNA BIEGŁE** (Jedyny punkt wiedzy o `@supabase/supabase-js`) |

### 3. Plan Faz Refaktoryzacji (Zgodny z Konwencją Projektu)

#### Faza 1: Utworzenie Kontraktów ACL i Portu (`IAuthService`)
- Utworzenie typu domenowego/aplikacyjnego `UserSession`, `AuthUser`, `AuthCredentials`, `AuthOutcome` w `src/Jadlify.Web/src/auth/IAuthService.ts`.
- Zdefiniowanie interfejsu `IAuthService`.

#### Faza 2: Implementacja Adaptera `SupabaseAuthAdapter`
- Utworzenie katalogu `src/Jadlify.Web/src/auth/adapters/`.
- Zaimplementowanie `SupabaseAuthAdapter.ts` z przeniesieniem mapowania błędów z `authErrors.ts` i konwersją typu `Session` na `UserSession`.

#### Faza 3: Refaktoryzacja Kontekstu i Dostawcy Sesji
- Przepięcie `SessionContext.ts` na `UserSession`.
- Zaktualizowanie `SessionProvider.tsx`, aby przyjmował instancję `IAuthService` poprzez wstrzykiwanie zależności lub fabrykę.

#### Faza 4: Refaktoryzacja Widoków UI i Klienta HTTP
- Przepięcie `LoginPage.tsx` na wywołania `authService.signIn()` oraz `authService.signUp()`.
- Przepięcie `AccountMenu.tsx` na wywołanie `authService.signOut()`.
- Przepięcie `apiClient.ts` na odczyt tokena z `authService.getSession()`.
- Usunięcie nieużywanych plików `lib/supabase.ts` oraz `authErrors.ts`.

#### Faza 5: Aktualizacja Testów Frontendowych i Weryfikacja
- Przepisywanie zaślepek testowych w `App.test.tsx`, `LoginPage.test.tsx`, `AccountMenu.test.tsx` oraz `SessionProvider.test.tsx` na czyste mocki interfejsu `IAuthService`.
- Weryfikacja braków przecieków komendą grep. Uruchomienie pełnego zestawu testów `npm test`.

---
