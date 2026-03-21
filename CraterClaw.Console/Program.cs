using System.Collections.ObjectModel;
using CraterClaw.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

var configPath = Path.Combine(AppContext.BaseDirectory, "craterclaw.json");

var configuration = new ConfigurationBuilder()
    .AddJsonFile(configPath, optional: false)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
var logPath = Path.Combine(logDirectory, "craterclaw-.log");

var aiEnabled = configuration.GetValue<bool>("aiLogging:enabled");
var aiPathConfig = configuration.GetValue<string>("aiLogging:path") ?? string.Empty;
var aiLogPath = ResolveAiLogPath(aiPathConfig, logDirectory);

static string ResolveAiLogPath(string configured, string defaultDirectory)
{
    if (string.IsNullOrWhiteSpace(configured))
        return Path.Combine(defaultDirectory, "ai-.log");
    var resolved = Path.IsPathRooted(configured)
        ? configured
        : Path.Combine(AppContext.BaseDirectory, configured);
    if (Directory.Exists(resolved) || resolved.EndsWith(Path.DirectorySeparatorChar) || resolved.EndsWith(Path.AltDirectorySeparatorChar))
        return Path.Combine(resolved.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), "ai-.log");
    return resolved;
}

var logConfig = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("System.Net.Http", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Logger(lc => lc
        .Filter.ByExcluding(e =>
            e.Properties.TryGetValue("SourceContext", out var sc) &&
            sc.ToString().Trim('"') == "CraterClaw.AiTraffic")
        .WriteTo.File(logPath, rollingInterval: RollingInterval.Day));

if (aiEnabled)
    logConfig = logConfig.WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e =>
            e.Properties.TryGetValue("SourceContext", out var sc) &&
            sc.ToString().Trim('"') == "CraterClaw.AiTraffic")
        .WriteTo.File(aiLogPath, rollingInterval: RollingInterval.Day));

Log.Logger = logConfig.CreateLogger();

var services = new ServiceCollection();
services.AddHttpClient();
services.AddCraterClawCore(configuration);
services.AddLogging(b => b.AddSerilog(dispose: true));

using var provider = services.BuildServiceProvider();
var providerOptions = provider.GetRequiredService<IOptions<ProviderOptions>>().Value;
var mcpOptions = provider.GetRequiredService<IOptions<McpOptions>>().Value;
var statusService = provider.GetRequiredService<IProviderStatusService>();
var modelListingService = provider.GetRequiredService<IModelListingService>();
var executionService = provider.GetRequiredService<IModelExecutionService>();
var mcpAvailabilityService = provider.GetRequiredService<IMcpAvailabilityService>();
var behaviorProfileService = provider.GetRequiredService<IBehaviorProfileService>();
var qBitTorrentPlugin = provider.GetRequiredService<QBitTorrentPlugin>();
var agenticExecutionService = provider.GetRequiredService<IAgenticExecutionService>();

try
{
    Console.WriteLine($"Log file: {logDirectory}");
    if (aiEnabled)
        Console.WriteLine($"AI log file: {aiLogPath}");

    var endpoints = providerOptions.Endpoints;

    if (endpoints.Count == 0)
    {
        Console.WriteLine("No configured endpoints found.");
        return;
    }

    var endpointList = endpoints
        .Select(kvp => new ProviderEndpoint(kvp.Key, kvp.Value.BaseUrl))
        .ToList();

    Console.WriteLine("Configured endpoints:");
    for (var i = 0; i < endpointList.Count; i++)
    {
        var defaultMarker = string.Equals(
            endpointList[i].Name,
            providerOptions.Active,
            StringComparison.OrdinalIgnoreCase)
            ? " (default)"
            : string.Empty;
        Console.WriteLine($"{i + 1}. {endpointList[i].Name}: {endpointList[i].BaseUrl}{defaultMarker}");
    }

    Console.Write("Select endpoint number (leave blank to use default): ");
    var selectedIndexInput = Console.ReadLine();

    ProviderEndpoint endpoint;
    if (string.IsNullOrWhiteSpace(selectedIndexInput))
    {
        endpoint = endpointList.FirstOrDefault(e =>
            string.Equals(e.Name, providerOptions.Active, StringComparison.OrdinalIgnoreCase))
            ?? endpointList[0];
    }
    else
    {
        if (!int.TryParse(selectedIndexInput, out var selectedIndex) ||
            selectedIndex < 1 ||
            selectedIndex > endpointList.Count)
        {
            Console.WriteLine($"Invalid selection '{selectedIndexInput}'. Expected a number between 1 and {endpointList.Count}.");
            return;
        }

        endpoint = endpointList[selectedIndex - 1];
    }

    Console.WriteLine($"Using endpoint: {endpoint.Name} ({endpoint.BaseUrl})");

    var status = await statusService.CheckStatusAsync(endpoint, CancellationToken.None);

    string? selectedModelName = null;

    if (status.IsReachable)
    {
        Console.WriteLine($"Reachable: {endpoint.BaseUrl}");

        IReadOnlyList<ModelDescriptor> models = [];
        try
        {
            models = await modelListingService.ListModelsAsync(endpoint, CancellationToken.None);
            if (models.Count == 0)
            {
                Console.WriteLine("No models downloaded on this endpoint.");
            }
            else
            {
                Console.WriteLine($"Available models ({models.Count}):");
                for (var i = 0; i < models.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {models[i].Name}  ({FormatSize(models[i].SizeBytes)})");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Model listing failed: {ex.Message}");
        }

        if (models.Count > 0)
        {
            Console.Write("Select model number (leave blank to skip): ");
            var modelIndexInput = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(modelIndexInput))
            {
                if (!int.TryParse(modelIndexInput, out var modelIndex) ||
                    modelIndex < 1 ||
                    modelIndex > models.Count)
                {
                    Console.WriteLine($"Invalid selection '{modelIndexInput}'. Expected a number between 1 and {models.Count}.");
                    return;
                }

                selectedModelName = models[modelIndex - 1].Name;
            }
        }

        if (!string.IsNullOrWhiteSpace(selectedModelName))
        {
            Console.Write("Enter prompt: ");
            var prompt = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(prompt))
            {
                try
                {
                    var executionRequest = new ExecutionRequest(
                        selectedModelName,
                        [new ConversationMessage(MessageRole.User, prompt.Trim())]);

                    var executionResponse = await executionService.ExecuteAsync(endpoint, executionRequest, CancellationToken.None);

                    Console.WriteLine("Response:");
                    Console.WriteLine(executionResponse.Content);

                    if (executionResponse.FinishReason == FinishReason.Length)
                    {
                        Console.WriteLine("(response truncated by token limit)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Execution failed: {ex.Message}");
                }
            }
        }
    }
    else
    {
        Console.WriteLine($"Unreachable: {endpoint.BaseUrl}");
        Console.WriteLine(status.ErrorMessage ?? "No error details provided.");
    }

    var mcpServers = mcpOptions.Servers
        .Select(kvp => new McpServerDefinition(
            kvp.Key,
            kvp.Value.Label,
            kvp.Value.Transport,
            kvp.Value.BaseUrl,
            kvp.Value.Command,
            kvp.Value.Args?.AsReadOnly(),
            kvp.Value.Env as IReadOnlyDictionary<string, string>,
            kvp.Value.Enabled))
        .ToList();

    if (mcpServers.Count == 0)
    {
        Console.WriteLine("No MCP servers configured.");
    }
    else
    {
        Console.WriteLine($"Configured MCP servers ({mcpServers.Count}):");
        for (var i = 0; i < mcpServers.Count; i++)
        {
            var enabledStatus = mcpServers[i].Enabled ? "enabled" : "disabled";
            Console.WriteLine($"{i + 1}. {mcpServers[i].Label}  ({mcpServers[i].Transport}, {enabledStatus})");
        }

        Console.Write("Select server number to check availability (leave blank to skip): ");
        var serverIndexInput = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(serverIndexInput))
        {
            if (!int.TryParse(serverIndexInput, out var serverIndex) ||
                serverIndex < 1 ||
                serverIndex > mcpServers.Count)
            {
                Console.WriteLine($"Invalid selection '{serverIndexInput}'. Expected a number between 1 and {mcpServers.Count}.");
            }
            else
            {
                var selectedServer = mcpServers[serverIndex - 1];
                var availability = await mcpAvailabilityService.CheckAvailabilityAsync(
                    selectedServer, CancellationToken.None);

                if (availability.IsAvailable)
                    Console.WriteLine($"Available: {availability.Name}");
                else
                    Console.WriteLine($"Unavailable: {availability.Name} - {availability.ErrorMessage}");
            }
        }
    }
    var profiles = behaviorProfileService.GetAll();
    Console.WriteLine($"Behavior profiles ({profiles.Count}):");
    for (var i = 0; i < profiles.Count; i++)
    {
        Console.WriteLine($"{i + 1}. [{profiles[i].Id}] {profiles[i].Name} - {profiles[i].Description}");
    }

    Console.Write("Select profile number (leave blank to skip): ");
    var profileIndexInput = Console.ReadLine();

    if (!string.IsNullOrWhiteSpace(profileIndexInput))
    {
        if (!int.TryParse(profileIndexInput, out var profileIndex) ||
            profileIndex < 1 ||
            profileIndex > profiles.Count)
        {
            Console.WriteLine($"Invalid selection '{profileIndexInput}'. Expected a number between 1 and {profiles.Count}.");
        }
        else
        {
            var selectedProfile = profiles[profileIndex - 1];
            var selectedProfileId = selectedProfile.Id;

            var permitted = selectedProfile.AllowedMcpServerNames;
            if (permitted.Count == 0)
            {
                Console.WriteLine("No plugin functions available for this profile.");
            }
            else
            {
                var functions = QBitTorrentPlugin.GetFunctionDescriptions();
                Console.WriteLine($"Available plugin functions ({functions.Count}):");
                for (var i = 0; i < functions.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {functions[i].Name} - {functions[i].Description}");
                }
            }

            if (!string.IsNullOrWhiteSpace(selectedModelName))
            {
                Console.Write("Enter task prompt (leave blank to skip): ");
                var taskPrompt = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(taskPrompt))
                {
                    try
                    {
                        IReadOnlyList<object> plugins = permitted.Count > 0
                            ? [qBitTorrentPlugin]
                            : [];

                        var agenticRequest = new AgenticRequest(
                            selectedModelName,
                            taskPrompt.Trim(),
                            plugins,
                            MaxIterations: 10,
                            StreamChunk: Console.Write);

                        Console.WriteLine("Response:");
                        var agenticResponse = await agenticExecutionService.ExecuteAsync(
                            endpoint, agenticRequest, CancellationToken.None);
                        Console.WriteLine();

                        foreach (var tool in agenticResponse.ToolsInvoked)
                            Console.WriteLine($"Tool: {tool}");
                        Console.WriteLine($"Tools invoked: {agenticResponse.ToolsInvoked.Count}");

                        if (agenticResponse.FinishReason == AgenticFinishReason.IterationLimitReached)
                            Console.WriteLine("(iteration limit reached)");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Agentic execution failed: {ex.Message}");
                    }
                }
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("An unexpected error occurred.");
    Console.WriteLine(ex.Message);
}
finally
{
    await Log.CloseAndFlushAsync();
}

static string FormatSize(long bytes)
{
    const long gb = 1_073_741_824;
    const long mb = 1_048_576;

    if (bytes >= gb)
        return $"{bytes / (double)gb:F1} GB";

    return $"{bytes / (double)mb:F1} MB";
}
