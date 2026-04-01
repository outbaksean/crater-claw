using System.Net;
using System.Text;
using System.Text.Json;

namespace CraterClaw.Core.Tests;

public class OllamaThinkingHandlerTests
{
    private static HttpMessageInvoker MakeInvoker(HttpResponseMessage fakeResponse)
    {
        var inner = new FakeInnerHandler(fakeResponse);
        var handler = new OllamaThinkingHandler { InnerHandler = inner };
        return new HttpMessageInvoker(handler);
    }

    [Fact]
    public async Task SendAsync_InjectsThinkTrue_WhenThinkingEnabled()
    {
        OllamaThinkingContext.ThinkingEnabled.Value = true;
        using var invoker = MakeInvoker(new HttpResponseMessage(HttpStatusCode.OK));

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/chat")
        {
            Content = new StringContent("{\"model\":\"qwen3\"}", Encoding.UTF8, "application/json")
        };

        using var _ = await invoker.SendAsync(request, CancellationToken.None);

        var body = await request.Content!.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.GetProperty("think").GetBoolean());
    }

    [Fact]
    public async Task SendAsync_InjectsThinkFalse_WhenThinkingDisabled()
    {
        OllamaThinkingContext.ThinkingEnabled.Value = false;
        using var invoker = MakeInvoker(new HttpResponseMessage(HttpStatusCode.OK));

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/chat")
        {
            Content = new StringContent("{\"model\":\"qwen3\"}", Encoding.UTF8, "application/json")
        };

        using var _ = await invoker.SendAsync(request, CancellationToken.None);

        var body = await request.Content!.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        Assert.False(doc.RootElement.GetProperty("think").GetBoolean());
    }

    [Fact]
    public async Task SendAsync_PreservesExistingFields()
    {
        OllamaThinkingContext.ThinkingEnabled.Value = false;
        using var invoker = MakeInvoker(new HttpResponseMessage(HttpStatusCode.OK));

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/chat")
        {
            Content = new StringContent("{\"model\":\"qwen3\",\"stream\":true}", Encoding.UTF8, "application/json")
        };

        using var _ = await invoker.SendAsync(request, CancellationToken.None);

        var body = await request.Content!.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        Assert.Equal("qwen3", doc.RootElement.GetProperty("model").GetString());
        Assert.True(doc.RootElement.GetProperty("stream").GetBoolean());
    }

    [Fact]
    public async Task SendAsync_NoContent_DoesNotThrow()
    {
        OllamaThinkingContext.ThinkingEnabled.Value = false;
        using var invoker = MakeInvoker(new HttpResponseMessage(HttpStatusCode.OK));

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/chat");

        using var _ = await invoker.SendAsync(request, CancellationToken.None);
    }

    private sealed class FakeInnerHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }
}
