using System.Net;
using System.Net.Http.Json;

namespace CraterClaw.Blazor.Tests;

internal sealed class TestHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    public TestHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        => _respond = respond;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => Task.FromResult(_respond(request));

    internal static HttpResponseMessage Json<T>(T value)
        => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
}
