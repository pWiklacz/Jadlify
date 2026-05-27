# When to Mock

Mock at **system boundaries** only:

- External APIs (Groq, OpenAI, Supabase, Lemon Squeezy)
- Databases (sometimes - prefer test DB or existing fixture)
- Time/randomness
- File system, clipboard, keyboard, audio devices, OS credential stores (sometimes)

Don't mock:

- Your own classes/modules
- Internal collaborators
- Anything you control

## Designing for Mockability

At system boundaries, design interfaces that are easy to mock:

**1. Use dependency injection**

Pass external dependencies in rather than creating them internally:

```csharp
// Easy to mock
public sealed class SessionManager
{
    private readonly ISupabaseAuthClient _authClient;

    public SessionManager(ISupabaseAuthClient authClient)
    {
        _authClient = authClient;
    }
}

// Hard to mock
public sealed class SessionManager
{
    public Task SignInAsync()
    {
        Supabase.Client client = new("real-url", "real-key");
        return client.Auth.SignIn(...);
    }
}
```

**2. Prefer SDK-style interfaces over generic fetchers**

Create specific functions for each external operation instead of one generic function with conditional logic:

```csharp
// GOOD: Each function is independently mockable
public interface ISupabaseAuthClient
{
    Task<SessionResult> SignInAsync(string email, string password, CancellationToken ct);

    Task SignOutAsync(CancellationToken ct);
}

// BAD: Mocking requires conditional logic inside the mock
public interface IRemoteClient
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct);
}
```

The SDK approach means:
- Each mock returns one specific shape
- No conditional logic in test setup
- Easier to see which endpoints a test exercises
- Type safety per endpoint

For API integration tests, prefer WireMock.Net over mocking `HttpMessageHandler` when verifying Groq/OpenAI/Supabase network behavior. It keeps the real `HttpClientFactory`, Polly, and serialization path in the test.
