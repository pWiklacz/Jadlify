> **TL;DR dla Agenta AI:**
> **Temat:** Wzorzec Result: `Result<T>` + `Error` record (w `phrAIse.Shared`), zakaz wyjątków dla kontroli przepływu, mapowanie na `ProblemDetails` (RFC 7807), `ResultLocalizationFilter` (IEndpointFilter).
> **Kiedy czytać:** OBOWIĄZKOWO przed implementacją jakiejkolwiek logiki biznesowej — ten wzorzec obowiązuje w CAŁYM projekcie (API + Avalonia). Definiuje jak zwracać i obsługiwać błędy.
> **Nie czytaj gdy:** Szukasz listy konkretnych kodów błędów — patrz `error-codes-registry.md`.

# Architectural Standard: Result Pattern

## 1. Purpose & Philosophy
Throughout the entire project (both in the backend API and the Avalonia UI client), **the use of Exceptions for control flow is STRICTLY FORBIDDEN**. 
Exceptions (`try-catch` blocks) must be reserved EXCLUSIVELY for critical, unforeseen system failures (e.g., lost database connection, power outage, disk read failures).

Any operation that might result in a predictable failure (e.g., unauthorized access, validation error, insufficient funds/quota) must explicitly return a `Result` or `Result<T>` object.

**Core Principle:** Keep it simple. Do not introduce complex functional monads (like deep chains of `.Bind()`, `.Map()`, `.Match()`). Use only the basic properties: `IsSuccess`, `IsFailure`, `Value`, and `Error`.

---

## 2. Base Implementation ([`phrAIse.Shared`](../../phrAIse.Shared/Result.cs) Project)

All Result pattern classes must reside in the shared project so that both the API and the Frontend speak the exact same language. The AI agent must implement a minimal, lightweight version:

### The `Error` Record
Defines the structure of an error.
```csharp
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "Value cannot be null.");
}
```

### The `Result<T>` Class
The primary data carrier.
```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public Error Error { get; }

    protected Result(bool isSuccess, Error error, T? value = default)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException();
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException();

        IsSuccess = isSuccess;
        Error = error;
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, Error.None, value);
    public static Result<T> Failure(Error error) => new(false, error);
}
```

---

## 3. Backend Rules (`DictationApp.Api`)

### 3.1. Business Logic (Handlers)
Every class executing business logic within the Vertical Slice Architecture must return a `Result<T>`.
```csharp
// EXAMPLE
public async Task<Result<string>> HandleAsync(AudioRequest request)
{
    if (request.AudioStream.Length == 0)
        return Result<string>.Failure(new Error("Audio.Empty", "The audio file is empty."));

    var text = await _groqService.Transcribe(request.AudioStream);
    return Result<string>.Success(text);
}
```

### 3.2. Endpoints & HTTP (API Boundary)
**Never serialize the `Result<T>` object directly as an HTTP response!** You must use an Extension Method in Minimal API that translates the `Result` into REST standards:
* If `IsSuccess` -> return `Results.Ok(result.Value)` (HTTP 200).
* If `IsFailure` -> return `Results.Problem(...)` (HTTP 400/404/409) populating the standard `ProblemDetails` with the Code and Message from the `Error` object.

### 3.3. Localized Error Responses via Endpoint Filters (i18n Integration)
The `Error.Code` field serves as the **localization key** for translating error messages based on the client's `Accept-Language` header. The `Error.Message` field is the **English fallback** used for debugging and logging — it is never sent directly to the client in production responses.

**Pipeline:**
1. Avalonia `HttpClient` sends `Accept-Language: pl-PL` header with every request.
2. `RequestLocalizationMiddleware` sets `CultureInfo.CurrentUICulture = pl-PL` for the request scope.
3. Business logic handler returns `Result<T>.Failure(new Error("Subscription.Expired", "Subscription has expired"))`.
4. `ResultLocalizationFilter` (an `IEndpointFilter`) intercepts the failed result.
5. Filter resolves `IStringLocalizer<SharedResources>` and calls `localizer["Subscription.Expired"]`.
6. `IStringLocalizer` looks up the key in `SharedResources.pl-PL.resx` → returns `"Twoja subskrypcja wygasła."`.
7. Filter returns `TypedResults.Problem(detail: "Twoja subskrypcja wygasła.", statusCode: 403)`.

**Rules:**
* Business logic handlers MUST NEVER construct localized messages. They return only semantic `Error.Code` values.
* The `Error.Code` must match a key in `SharedResources.resx` (backend localization files).
* See 👉 [i18n Standard](../standards/i18n-standard.md) for full architecture details.

---

## 4. Frontend Rules (`DictationApp.Desktop` / MVVM)

### 4.1. Client Services (HttpClients)
Services communicating with the API must catch HTTP errors (e.g., deserializing the `ProblemDetails` response) and wrap them back into a `Result<T>` object. The ViewModel should never know about HTTP status codes.

### 4.2. ViewModels (The Golden Rule of Clean UI)
Never use `try-catch` blocks in ViewModels to handle API errors. The code must flow linearly.

```csharp
// EXAMPLE OF A CLEAN VIEWMODEL
[RelayCommand]
public async Task TranscribeAudioAsync()
{
    IsLoading = true;
    ErrorMessage = string.Empty;

    // Method returns Result<string>
    var result = await _audioApiClient.SendAudioAsync(LocalFilePath);

    IsLoading = false;

    if (result.IsFailure)
    {
        // Display the error in the UI
        ErrorMessage = result.Error.Message;
        return;
    }

    // Success - update UI
    TranscribedText = result.Value;
}
```

## 5. Directives for the AI Agent
1. **Validation:** If you are validating input data -> return `Result.Failure`.
2. **Database Queries:** If a user/record is not found in Supabase -> return `Result.Failure(new Error("Record.NotFound", "..."))`.
3. **External AI APIs:** If the Groq/OpenAI API is unavailable (Timeout) -> catch the `HttpRequestException` at the lowest service level and map it to `Result.Failure(new Error("Network", "Failed to connect to the AI service"))`.
4. **Localization:** Never hardcode user-facing error messages in handlers. Use semantic `Error.Code` values that map to `.resx` keys in the backend's `SharedResources` files.
```