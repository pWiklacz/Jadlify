# Good and Bad Tests

## Good Tests

**Integration-style**: Test through real interfaces, not mocks of internal parts.

```csharp
// GOOD: Tests observable API behavior through the public HTTP surface.
[Fact]
public async Task POST_Transcribe_With_Unknown_Profile_Returns_400_ProfileUnknown()
{
    using HttpRequestMessage request = BuildAuthorizedRequest(profileHeader: "NonExistent");

    using HttpResponseMessage response = await _client.SendAsync(request);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    await AssertProblemDetailsErrorCode(response, "Dictation.ProfileUnknown");
}
```

Characteristics:

- Tests behavior users/callers care about
- Uses public API only
- Survives internal refactors
- Describes WHAT, not HOW
- One logical assertion per test

## Bad Tests

**Implementation-detail tests**: Coupled to internal structure.

```csharp
// BAD: Verifies an internal call instead of the observable result.
[Fact]
public async Task Transcribe_Calls_ProfileResolver()
{
    IProfileResolver resolver = Substitute.For<IProfileResolver>();

    await _service.TranscribeAsync(request, CancellationToken.None);

    await resolver.Received(1).ResolveAsync("Pure_English", Arg.Any<CancellationToken>());
}
```

Red flags:

- Mocking internal collaborators
- Testing private methods
- Asserting on call counts/order
- Test breaks when refactoring without behavior change
- Test name describes HOW not WHAT
- Verifying through external means instead of interface

```csharp
// BAD: Bypasses interface to verify
[Fact]
public async Task CreateSession_Saves_Row()
{
    await _client.PostAsJsonAsync("/api/v1/sessions", request);

    SessionRow? row = await _db.Sessions.FindAsync("expected-id");
    row.Should().NotBeNull();
}

// GOOD: Verifies through public behavior
[Fact]
public async Task CreateSession_Makes_Session_Retrievable()
{
    using HttpResponseMessage create = await _client.PostAsJsonAsync("/api/v1/sessions", request);
    CreateSessionResponse body = await create.Content.ReadFromJsonAsync<CreateSessionResponse>();

    using HttpResponseMessage get = await _client.GetAsync($"/api/v1/sessions/{body.Id}");

    get.StatusCode.Should().Be(HttpStatusCode.OK);
}
```
