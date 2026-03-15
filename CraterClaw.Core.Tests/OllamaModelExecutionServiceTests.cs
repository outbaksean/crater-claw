using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace CraterClaw.Core.Tests;

public sealed class OllamaModelExecutionServiceTests
{
    private static readonly ProviderEndpoint TestEndpoint = new("local", "http://localhost:11434");

    [Fact]
    public async Task ExecuteAsync_ReturnsResponse_WithContentAndStopReason()
    {
        const string json = """
            {
              "model": "llama3.2:latest",
              "message": { "role": "assistant", "content": "The sky is blue due to Rayleigh scattering." },
              "done": true,
              "done_reason": "stop"
            }
            """;

        using var client = CreateClient(HttpStatusCode.OK, json);
        var service = new OllamaModelExecutionService(client, NullLogger<OllamaModelExecutionService>.Instance);

        var request = new ExecutionRequest("llama3.2:latest", [new ConversationMessage(MessageRole.User, "Why is the sky blue?")]);
        var response = await service.ExecuteAsync(TestEndpoint, request, CancellationToken.None);

        Assert.Equal("The sky is blue due to Rayleigh scattering.", response.Content);
        Assert.Equal("llama3.2:latest", response.ModelName);
        Assert.Equal(FinishReason.Stop, response.FinishReason);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsLengthFinishReason_WhenDoneReasonIsLength()
    {
        const string json = """
            {
              "model": "llama3.2:latest",
              "message": { "role": "assistant", "content": "Partial response..." },
              "done": true,
              "done_reason": "length"
            }
            """;

        using var client = CreateClient(HttpStatusCode.OK, json);
        var service = new OllamaModelExecutionService(client, NullLogger<OllamaModelExecutionService>.Instance);

        var request = new ExecutionRequest("llama3.2:latest", [new ConversationMessage(MessageRole.User, "Tell me everything.")]);
        var response = await service.ExecuteAsync(TestEndpoint, request, CancellationToken.None);

        Assert.Equal(FinishReason.Length, response.FinishReason);
    }

    [Fact]
    public async Task ExecuteAsync_SendsCorrectRequestBody()
    {
        const string responseJson = """
            {
              "model": "llama3.2:latest",
              "message": { "role": "assistant", "content": "Hello." },
              "done": true,
              "done_reason": "stop"
            }
            """;

        HttpRequestMessage? captured = null;
        string? capturedBody = null;

        using var client = CreateClient(async (req, _) =>
        {
            captured = req;
            capturedBody = await req.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        });

        var service = new OllamaModelExecutionService(client, NullLogger<OllamaModelExecutionService>.Instance);
        var request = new ExecutionRequest("llama3.2:latest", [new ConversationMessage(MessageRole.User, "Hi")]);

        await service.ExecuteAsync(TestEndpoint, request, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.EndsWith("/api/chat", captured!.RequestUri!.AbsolutePath, StringComparison.Ordinal);

        using var doc = JsonDocument.Parse(capturedBody!);
        var root = doc.RootElement;

        Assert.Equal("llama3.2:latest", root.GetProperty("model").GetString());
        Assert.False(root.GetProperty("stream").GetBoolean());
        var messages = root.GetProperty("messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("user", messages[0].GetProperty("role").GetString());
        Assert.Equal("Hi", messages[0].GetProperty("content").GetString());
        Assert.False(root.TryGetProperty("options", out _), "options should be omitted when no generation parameters are set");
    }

    [Fact]
    public async Task ExecuteAsync_IncludesOptions_WhenGenerationParametersAreSet()
    {
        const string responseJson = """
            {
              "model": "llama3.2:latest",
              "message": { "role": "assistant", "content": "Hi." },
              "done": true,
              "done_reason": "stop"
            }
            """;

        string? capturedBody = null;

        using var client = CreateClient(async (req, _) =>
        {
            capturedBody = await req.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        });

        var service = new OllamaModelExecutionService(client, NullLogger<OllamaModelExecutionService>.Instance);
        var request = new ExecutionRequest(
            "llama3.2:latest",
            [new ConversationMessage(MessageRole.User, "Hi")],
            Temperature: 0.5,
            MaxTokens: 256);

        await service.ExecuteAsync(TestEndpoint, request, CancellationToken.None);

        using var doc = JsonDocument.Parse(capturedBody!);
        var options = doc.RootElement.GetProperty("options");

        Assert.Equal(0.5, options.GetProperty("temperature").GetDouble(), precision: 5);
        Assert.Equal(256, options.GetProperty("num_predict").GetInt32());
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsInvalidOperationException_ForNonSuccessHttpStatus()
    {
        using var client = CreateClient(HttpStatusCode.NotFound, string.Empty);
        var service = new OllamaModelExecutionService(client, NullLogger<OllamaModelExecutionService>.Instance);

        var request = new ExecutionRequest("missing-model", [new ConversationMessage(MessageRole.User, "Hello")]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExecuteAsync(TestEndpoint, request, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsInvalidOperationException_ForMalformedJson()
    {
        using var client = CreateClient(HttpStatusCode.OK, "{ not valid }");
        var service = new OllamaModelExecutionService(client, NullLogger<OllamaModelExecutionService>.Instance);

        var request = new ExecutionRequest("llama3.2:latest", [new ConversationMessage(MessageRole.User, "Hello")]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExecuteAsync(TestEndpoint, request, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesCancellation_WhenTokenIsCancelled()
    {
        using var client = CreateClient(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var service = new OllamaModelExecutionService(client, NullLogger<OllamaModelExecutionService>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var request = new ExecutionRequest("llama3.2:latest", [new ConversationMessage(MessageRole.User, "Hello")]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ExecuteAsync(TestEndpoint, request, cts.Token));
    }

    private static HttpClient CreateClient(HttpStatusCode statusCode, string responseBody)
    {
        return CreateClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            }));
    }

    private static HttpClient CreateClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        return new HttpClient(new DelegatingTestHandler(handler));
    }

    private sealed class DelegatingTestHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return handler(request, cancellationToken);
        }
    }
}
