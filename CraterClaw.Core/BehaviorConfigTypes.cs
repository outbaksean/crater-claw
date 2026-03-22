namespace CraterClaw.Core;

public sealed class PluginEntry
{
    public string Name { get; set; } = "";
    public List<string> Tools { get; set; } = [];
    public Dictionary<string, string> Config { get; set; } = [];
}

public sealed class BehaviorEntry
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string SystemPrompt { get; set; } = "";
    public string? PreferredProviderName { get; set; }
    public string? PreferredModelName { get; set; }
    public List<PluginEntry> Plugins { get; set; } = [];
}
