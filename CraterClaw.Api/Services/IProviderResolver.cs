using CraterClaw.Core;

namespace CraterClaw.Api.Services;

internal interface IProviderResolver
{
    ProviderEndpoint? Resolve(string name);
    IEnumerable<ProviderEndpoint> GetAll();
}
