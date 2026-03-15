using CraterClaw.Core;
using Microsoft.Extensions.DependencyInjection;

var configurationPathInput = args.FirstOrDefault();

if (string.IsNullOrWhiteSpace(configurationPathInput))
{
    Console.Write("Enter provider config file path (leave blank for ./provider-config.json): ");
    configurationPathInput = Console.ReadLine();
}

var configurationPath = string.IsNullOrWhiteSpace(configurationPathInput)
    ? Path.Combine(Environment.CurrentDirectory, "provider-config.json")
    : configurationPathInput.Trim();

var services = new ServiceCollection();
services.AddHttpClient();
services.AddCraterClawCore(configurationPath);

using var provider = services.BuildServiceProvider();
var configurationService = provider.GetRequiredService<IProviderConfigurationService>();
var statusService = provider.GetRequiredService<IProviderStatusService>();
var modelListingService = provider.GetRequiredService<IModelListingService>();
var executionService = provider.GetRequiredService<IModelExecutionService>();

try
{
    var configuration = await configurationService.LoadAsync(CancellationToken.None);
    var endpoints = configuration.Endpoints;

    if (endpoints.Count == 0)
    {
        Console.WriteLine("No configured endpoints found.");
        return;
    }

    Console.WriteLine($"Loaded provider config: {configurationPath}");
    Console.WriteLine("Configured endpoints:");
    for (var i = 0; i < endpoints.Count; i++)
    {
        var configuredEndpoint = endpoints[i];
        var activeMarker = string.Equals(
            configuredEndpoint.Name,
            configuration.ActiveProviderName,
            StringComparison.OrdinalIgnoreCase)
            ? " (active)"
            : string.Empty;

        Console.WriteLine($"{i + 1}. {configuredEndpoint.Name}: {configuredEndpoint.BaseUrl}{activeMarker}");
    }

    Console.Write("Select endpoint number (leave blank to keep active): ");
    var selectedIndexInput = Console.ReadLine();

    ProviderEndpoint endpoint;
    if (string.IsNullOrWhiteSpace(selectedIndexInput))
    {
        endpoint = await configurationService.GetActiveEndpointAsync(CancellationToken.None);
    }
    else
    {
        if (!int.TryParse(selectedIndexInput, out var selectedIndex) ||
            selectedIndex < 1 ||
            selectedIndex > endpoints.Count)
        {
            Console.WriteLine($"Invalid selection '{selectedIndexInput}'. Expected a number between 1 and {endpoints.Count}.");
            return;
        }

        var selectedEndpoint = endpoints[selectedIndex - 1];
        endpoint = await configurationService.SetActiveEndpointAsync(selectedEndpoint.Name, CancellationToken.None);
    }

    Console.WriteLine($"Using endpoint: {endpoint.Name} ({endpoint.BaseUrl})");

    var status = await statusService.CheckStatusAsync(endpoint, CancellationToken.None);

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

        string? selectedModelName = null;
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
}
catch (Exception ex)
{
    Console.WriteLine("An unexpected error occurred while checking provider status.");
    Console.WriteLine(ex.Message);
}

static string FormatSize(long bytes)
{
    const long gb = 1_073_741_824;
    const long mb = 1_048_576;

    if (bytes >= gb)
        return $"{bytes / (double)gb:F1} GB";

    return $"{bytes / (double)mb:F1} MB";
}
