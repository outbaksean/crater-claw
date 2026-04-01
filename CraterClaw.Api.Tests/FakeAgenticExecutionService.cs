using CraterClaw.Core;

namespace CraterClaw.Api.Tests;

internal sealed class FakeAgenticExecutionService(
    AgenticResponse response,
    IReadOnlyList<string>? chunks = null,
    IReadOnlyList<string>? thinkingChunks = null) : IAgenticExecutionService
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
        return response;
    }
}
