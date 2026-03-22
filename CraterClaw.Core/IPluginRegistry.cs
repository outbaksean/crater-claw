using Microsoft.SemanticKernel;

namespace CraterClaw.Core;

public interface IPluginRegistry
{
    // Returns pre-filtered KernelPlugin instances ready to add to a kernel.
    // Empty Tools list in a PluginBinding means all tools in the plugin are included.
    // Unknown plugin names are skipped (logged as warning).
    IReadOnlyList<KernelPlugin> Resolve(IEnumerable<PluginBinding> plugins);
}
