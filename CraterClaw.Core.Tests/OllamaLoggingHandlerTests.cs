using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace CraterClaw.Core.Tests;

public class OllamaLoggingHandlerTests
{
    [Fact]
    public async Task SendAsync_LogsRequestBody()
    {
        var loggerFactory = new CaptureLoggerFactory();
        var innerHandler = new FakeInnerHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });
        using var handler = new OllamaLoggingHandler(loggerFactory) { InnerHandler = innerHandler };
        using var invoker = new HttpMessageInvoker(handler);

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/chat")
        {
            Content = new StringContent("{\"model\":\"test\"}", Encoding.UTF8, "application/json")
        };

        using var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Contains(loggerFactory.Logger.Messages,
            m => m.Contains("[REQUEST]") && m.Contains("\"model\":\"test\""));
    }

    [Fact]
    public async Task SendAsync_LogsResponseBody()
    {
        var loggerFactory = new CaptureLoggerFactory();
        var innerHandler = new FakeInnerHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"response\":\"hello\"}", Encoding.UTF8, "application/json")
        });
        using var handler = new OllamaLoggingHandler(loggerFactory) { InnerHandler = innerHandler };
        using var invoker = new HttpMessageInvoker(handler);

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/chat");
        var response = await invoker.SendAsync(request, CancellationToken.None);
        await response.Content.ReadAsStringAsync();
        response.Dispose();

        Assert.Contains(loggerFactory.Logger.Messages,
            m => m.Contains("[RESPONSE]") && m.Contains("\"response\":\"hello\""));
    }

    [Fact]
    public async Task SendAsync_ResponseBodyPassesThrough()
    {
        var loggerFactory = new CaptureLoggerFactory();
        const string expected = "{\"response\":\"hello\"}";
        var innerHandler = new FakeInnerHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expected, Encoding.UTF8, "application/json")
        });
        using var handler = new OllamaLoggingHandler(loggerFactory) { InnerHandler = innerHandler };
        using var invoker = new HttpMessageInvoker(handler);

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/chat");
        using var response = await invoker.SendAsync(request, CancellationToken.None);
        var actual = await response.Content.ReadAsStringAsync();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task SendAsync_NullRequestContent_DoesNotThrow()
    {
        var loggerFactory = new CaptureLoggerFactory();
        var innerHandler = new FakeInnerHandler(new HttpResponseMessage(HttpStatusCode.OK));
        using var handler = new OllamaLoggingHandler(loggerFactory) { InnerHandler = innerHandler };
        using var invoker = new HttpMessageInvoker(handler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/tags");
        using var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.DoesNotContain(loggerFactory.Logger.Messages, m => m.Contains("[REQUEST]"));
    }

    [Fact]
    public async Task SendAsync_NullResponseContent_DoesNotThrow()
    {
        var loggerFactory = new CaptureLoggerFactory();
        var innerHandler = new FakeInnerHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        using var handler = new OllamaLoggingHandler(loggerFactory) { InnerHandler = innerHandler };
        using var invoker = new HttpMessageInvoker(handler);

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/chat");
        using var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.DoesNotContain(loggerFactory.Logger.Messages, m => m.Contains("[RESPONSE]"));
    }

    private sealed class FakeInnerHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private sealed class CaptureLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }

    private sealed class CaptureLoggerFactory : ILoggerFactory
    {
        public CaptureLogger Logger { get; } = new();
        public ILogger CreateLogger(string categoryName) => Logger;
        public void AddProvider(ILoggerProvider provider) { }
        public void Dispose() { }
    }
}
