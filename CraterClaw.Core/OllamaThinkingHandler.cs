using System.Text;
using System.Text.Json.Nodes;

namespace CraterClaw.Core;

internal sealed class OllamaThinkingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is not null)
        {
            await request.Content.LoadIntoBufferAsync(cancellationToken);
            var body = await request.Content.ReadAsStringAsync(cancellationToken);
            if (JsonNode.Parse(body) is JsonObject json)
            {
                json["think"] = OllamaThinkingContext.ThinkingEnabled.Value;
                request.Content = new StringContent(json.ToJsonString(), Encoding.UTF8, "application/json");
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
