using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace CraterClaw.Core;

internal sealed class DefaultPluginRegistry(
    IReadOnlyDictionary<string, Func<IReadOnlyDictionary<string, string>, object>> factories,
    ILogger<DefaultPluginRegistry> logger) : IPluginRegistry
{
    public IReadOnlyList<KernelPlugin> Resolve(IEnumerable<PluginBinding> plugins)
    {
        var result = new List<KernelPlugin>();

        foreach (var binding in plugins)
        {
            if (!factories.TryGetValue(binding.Name, out var factory))
            {
                logger.LogWarning("Plugin '{PluginName}' is not registered in the plugin registry", binding.Name);
                continue;
            }

            var instance = factory(binding.Config);
            var fullPlugin = KernelPluginFactory.CreateFromObject(instance, binding.Name);

            if (binding.Tools.Count == 0)
            {
                result.Add(fullPlugin);
                continue;
            }

            var filteredFunctions = new List<KernelFunction>();
            foreach (var toolName in binding.Tools)
            {
                var function = fullPlugin.FirstOrDefault(f =>
                    string.Equals(f.Name, toolName, StringComparison.OrdinalIgnoreCase));
                if (function is not null)
                    filteredFunctions.Add(function);
                else
                    logger.LogWarning("Tool '{ToolName}' not found in plugin '{PluginName}'", toolName, binding.Name);
            }

            result.Add(KernelPluginFactory.CreateFromFunctions(binding.Name, filteredFunctions));
        }

        return result;
    }
}
