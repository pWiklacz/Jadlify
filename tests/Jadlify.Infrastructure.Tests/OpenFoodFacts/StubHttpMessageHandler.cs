using System.Net;
using System.Text;

namespace Jadlify.Infrastructure.Tests.OpenFoodFacts;

/// <summary>
/// A controllable <see cref="HttpMessageHandler"/> for adapter tests: returns a canned
/// response (or throws a canned exception) so every OFF outcome — found, partial, only-kJ,
/// <c>status: 0</c>, 404/302, timeout, malformed JSON — can be exercised without a network.
/// Used as the whole pipeline (no redirect following), so a 3xx is returned verbatim.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    private StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    public Uri? LastRequestUri { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;
        return Task.FromResult(_responder(request));
    }

    public static StubHttpMessageHandler Json(HttpStatusCode statusCode, string json) =>
        new(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });

    public static StubHttpMessageHandler Status(HttpStatusCode statusCode) =>
        new(_ => new HttpResponseMessage(statusCode));

    public static StubHttpMessageHandler Throws(Exception exception) =>
        new(_ => throw exception);
}
