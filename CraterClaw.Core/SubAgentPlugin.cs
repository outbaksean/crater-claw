namespace CraterClaw.Core;

internal sealed class SubAgentPlugin(
    string profileId,
    string functionName,
    string description,
    IBehaviorProfileService profileService,
    IAgenticExecutionService agenticService,
    IPluginRegistry pluginRegistry)
{
    public string FunctionName => functionName;
    public string Description => description;

    public async Task<string> RunAsync(string prompt, CancellationToken cancellationToken)
    {
        var endpoint = PluginExecutionContext.CurrentEndpoint.Value;
        if (endpoint is null)
            return "Error: no provider endpoint is available in the current execution context.";

        var profile = profileService.GetById(profileId);
        if (profile is null)
            return $"Error: behavior profile '{profileId}' was not found.";

        var childPlugins = pluginRegistry.Resolve(profile.Plugins);

        var childStreamStart = PluginExecutionContext.ChildStreamStart.Value;
        var childStreamChunk = PluginExecutionContext.ChildStreamChunk.Value;
        var childStreamThinking = PluginExecutionContext.ChildStreamThinking.Value;

        if (childStreamStart is not null)
            await childStreamStart(functionName, prompt);

        var request = new AgenticRequest(
            ModelName: profile.PreferredModelName ?? string.Empty,
            Prompt: prompt,
            Plugins: childPlugins,
            MaxIterations: 10,
            SystemPrompt: profile.SystemPrompt,
            MaxContext: profile.MaxContext,
            Depth: PluginExecutionContext.CurrentDepth.Value + 1,
            StreamChunk: childStreamChunk is not null
                ? text => childStreamChunk(functionName, text)
                : null,
            StreamThinkingChunk: childStreamThinking is not null
                ? text => childStreamThinking(functionName, text)
                : null);

        var response = await agenticService.ExecuteAsync(endpoint, request, cancellationToken);
        return response.Content;
    }
}
