using CraterClaw.Core;

namespace CraterClaw.Api.Tests;

internal sealed class FakeAgenticExecutionService(
    AgenticResponse response,
    IReadOnlyList<string>? chunks = null,
    IReadOnlyList<string>? thinkingChunks = null,
    IReadOnlyList<(string Source, string Text)>? childChunks = null) : IAgenticExecutionService
{
    public async Task<AgenticResponse> ExecuteAsync(
        ProviderEndpoint endpoint,
        AgenticRequest request,
        CancellationToken cancellationToken)
    {
        if (request.StreamThinkingChunk is not null && thinkingChunks is not null)
            foreach (var chunk in thinkingChunks)
                await request.StreamThinkingChunk(chunk);
        if (request.StreamChunk is not null && chunks is not null)
            foreach (var chunk in chunks)
                await request.StreamChunk(chunk);
        if (request.StreamChildChunk is not null && childChunks is not null)
            foreach (var (source, text) in childChunks)
                await request.StreamChildChunk(source, text);
        return response;
    }
}
