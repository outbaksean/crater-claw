using CraterClaw.Core;
using Microsoft.Extensions.Options;

namespace CraterClaw.Api.Services;

internal sealed class ProviderResolver : IProviderResolver
{
    private readonly IOptions<ProviderOptions> _opts;

    public ProviderResolver(IOptions<ProviderOptions> opts) => _opts = opts;

    public ProviderEndpoint? Resolve(string name)
    {
        if (!_opts.Value.Endpoints.TryGetValue(name, out var endpointOpts))
            return null;
        return new ProviderEndpoint(name, endpointOpts.BaseUrl);
    }

    public IEnumerable<ProviderEndpoint> GetAll() =>
        _opts.Value.Endpoints.Select(kvp => new ProviderEndpoint(kvp.Key, kvp.Value.BaseUrl));
}
