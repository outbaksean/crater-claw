using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace CraterClaw.Core;

internal sealed class OllamaModelExecutionService(
    HttpClient httpClient,
    ILogger<OllamaModelExecutionService> logger) : IModelExecutionService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<ExecutionResponse> ExecuteAsync(
        ProviderEndpoint endpoint,
        ExecutionRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Executing model {ModelName} at endpoint {EndpointName}", request.ModelName, endpoint.Name);

        if (!Uri.TryCreate(endpoint.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("BaseUrl is not a valid absolute URI.");
        }

        var chatUri = new Uri(baseUri, "/api/chat");

        OllamaOptions? options = null;
        if (request.Temperature is not null || request.MaxTokens is not null)
        {
            options = new OllamaOptions(request.Temperature, request.MaxTokens);
        }

        var messages = request.Messages
            .Select(m => new OllamaMessage(m.Role.ToString().ToLowerInvariant(), m.Content))
            .ToList();

        var body = new OllamaChatRequest(request.ModelName, messages, Stream: false, options);
        var json = JsonSerializer.Serialize(body, SerializerOptions);

        logger.LogDebug("Request body: {RequestJson}", json);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage httpResponse;
        try
        {
            httpResponse = await httpClient.PostAsync(chatUri, content, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("Model execution failed: {ErrorMessage}", ex.Message);
            throw new InvalidOperationException($"Model execution request failed: {ex.Message}", ex);
        }

        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorMessage = $"Provider returned HTTP {(int)httpResponse.StatusCode} ({httpResponse.StatusCode}).";
            logger.LogError("Model execution failed: {ErrorMessage}", errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        await using var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);

        OllamaChatResponse? response;
        try
        {
            response = await JsonSerializer.DeserializeAsync<OllamaChatResponse>(
                stream,
                SerializerOptions,
                cancellationToken);
        }
        catch (JsonException ex)
        {
            logger.LogError("Model execution failed: {ErrorMessage}", ex.Message);
            throw new InvalidOperationException("Model execution response JSON is invalid.", ex);
        }

        if (response is null)
        {
            logger.LogError("Model execution failed: response was empty");
            throw new InvalidOperationException("Model execution response was empty.");
        }

        var finishReason = response.DoneReason switch
        {
            "length" => FinishReason.Length,
            _ => FinishReason.Stop
        };

        logger.LogInformation("Model {ModelName} finished with reason {FinishReason}", request.ModelName, finishReason);

        return new ExecutionResponse(
            response.Message?.Content ?? string.Empty,
            response.Model ?? string.Empty,
            finishReason);
    }

    private sealed record OllamaChatRequest(
        string Model,
        List<OllamaMessage> Messages,
        bool Stream,
        OllamaOptions? Options);

    private sealed record OllamaMessage(string Role, string Content);

    private sealed record OllamaOptions(double? Temperature, int? NumPredict);

    private sealed record OllamaChatResponse(
        string? Model,
        OllamaMessage? Message,
        bool Done,
        string? DoneReason);
}
