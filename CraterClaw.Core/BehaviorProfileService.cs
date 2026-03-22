using Microsoft.Extensions.Options;

namespace CraterClaw.Core;

internal sealed class BehaviorProfileService : IBehaviorProfileService
{
    private readonly IReadOnlyList<BehaviorProfile> _profiles;

    public BehaviorProfileService(IOptions<Dictionary<string, BehaviorEntry>> options)
    {
        _profiles = options.Value
            .Select(kvp => new BehaviorProfile(
                kvp.Key,
                kvp.Value.Name,
                kvp.Value.Description,
                kvp.Value.SystemPrompt,
                kvp.Value.PreferredProviderName,
                kvp.Value.PreferredModelName,
                kvp.Value.Plugins.Select(p => new PluginBinding(p.Name, [.. p.Tools], p.Config)).ToList()))
            .ToList();
    }

    public IReadOnlyList<BehaviorProfile> GetAll() => _profiles;

    public BehaviorProfile? GetById(string id) =>
        _profiles.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
}
