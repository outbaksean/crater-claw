using CraterClaw.Core;
using Microsoft.Extensions.DependencyInjection;

var endpointInput = args.FirstOrDefault();

if (string.IsNullOrWhiteSpace(endpointInput))
{
    Console.Write("Enter Ollama base URL: ");
    endpointInput = Console.ReadLine();
}

if (string.IsNullOrWhiteSpace(endpointInput))
{
    Console.WriteLine("No endpoint provided. Exiting.");
    return;
}

var services = new ServiceCollection();
services.AddHttpClient();
services.AddCraterClawCore();

using var provider = services.BuildServiceProvider();
var statusService = provider.GetRequiredService<IProviderStatusService>();

try
{
    var endpoint = new ProviderEndpoint("ollama", endpointInput.Trim());
    var status = await statusService.CheckStatusAsync(endpoint, CancellationToken.None);

    if (status.IsReachable)
    {
        Console.WriteLine($"Reachable: {endpoint.BaseUrl}");
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
