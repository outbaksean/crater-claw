using Microsoft.Extensions.Logging;

namespace CraterClaw.Core;

internal sealed class OllamaLoggingHandler(ILoggerFactory loggerFactory) : DelegatingHandler
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("CraterClaw.AiTraffic.Raw");

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is not null)
        {
            await request.Content.LoadIntoBufferAsync(cancellationToken);
            var requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("[REQUEST] {Body}", requestBody);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.Content is not null)
            response.Content = new TeeHttpContent(response.Content, _logger);

        return response;
    }
}
