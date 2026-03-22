using System.Text.Json.Serialization;
using CraterClaw.Core;
using Microsoft.Extensions.Options;
using Serilog;

var (filteredArgs, configFilePath) = ParseArgs(args, Path.Combine(AppContext.BaseDirectory, "craterclaw.json"));
var builder = WebApplication.CreateBuilder(filteredArgs);

// Rebuild configuration in explicit priority order so user secrets and env vars
// override the committed placeholder values in craterclaw.json.
builder.Configuration.Sources.Clear();
builder.Configuration.SetBasePath(AppContext.BaseDirectory);
builder.Configuration.AddJsonFile(configFilePath, optional: true);
builder.Configuration.AddUserSecrets<Program>();
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(filteredArgs);

static (string[] Args, string ConfigPath) ParseArgs(string[] args, string defaultPath)
{
    var envPath = Environment.GetEnvironmentVariable("CRATERCLAW_CONFIG");
    if (!string.IsNullOrWhiteSpace(envPath))
        return (args, envPath);

    var filtered = new List<string>();
    var configPath = defaultPath;
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i] == "--config" && i + 1 < args.Length)
        {
            configPath = args[i + 1];
            i++;
        }
        else
        {
            filtered.Add(args[i]);
        }
    }
    return (filtered.ToArray(), configPath);
}

var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
var logPath = Path.Combine(logDirectory, "craterclaw-api-.log");

var aiEnabled = builder.Configuration.GetValue<bool>("aiLogging:enabled");
var aiPathConfig = builder.Configuration.GetValue<string>("aiLogging:path") ?? string.Empty;
var aiLogPath = ResolveAiLogPath(aiPathConfig, logDirectory);

static string ResolveAiLogPath(string configured, string defaultDirectory)
{
    if (string.IsNullOrWhiteSpace(configured))
        return Path.Combine(defaultDirectory, "ai-api-.log");
    var resolved = Path.IsPathRooted(configured)
        ? configured
        : Path.Combine(AppContext.BaseDirectory, configured);
    if (Directory.Exists(resolved) || resolved.EndsWith(Path.DirectorySeparatorChar) || resolved.EndsWith(Path.AltDirectorySeparatorChar))
        return Path.Combine(resolved.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), "ai-.log");
    return resolved;
}

var logConfig = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
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

builder.Host.UseSerilog(logConfig.CreateLogger(), dispose: true);

builder.Services.AddCraterClawCore(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

app.MapGet("/api/providers", (IOptions<ProviderOptions> opts) =>
{
    var endpoints = opts.Value.Endpoints
        .Select(kvp => new ProviderEndpointResponse(kvp.Key, kvp.Value.BaseUrl))
        .ToList();
    return Results.Ok(endpoints);
});

app.MapGet("/api/providers/{name}/status", async (
    string name,
    IOptions<ProviderOptions> opts,
    IProviderStatusService statusService,
    CancellationToken cancellationToken) =>
{
    if (!opts.Value.Endpoints.TryGetValue(name, out var endpointOpts))
        return Results.NotFound();

    var endpoint = new ProviderEndpoint(name, endpointOpts.BaseUrl);
    var status = await statusService.CheckStatusAsync(endpoint, cancellationToken);
    return Results.Ok(new ProviderStatusResponse(status.IsReachable, status.ErrorMessage));
});

app.MapGet("/api/providers/{name}/models", async (
    string name,
    IOptions<ProviderOptions> opts,
    IModelListingService modelListingService,
    CancellationToken cancellationToken) =>
{
    if (!opts.Value.Endpoints.TryGetValue(name, out var endpointOpts))
        return Results.NotFound();

    var endpoint = new ProviderEndpoint(name, endpointOpts.BaseUrl);
    var models = await modelListingService.ListModelsAsync(endpoint, cancellationToken);
    return Results.Ok(models.Select(m => new ModelApiItem(m.Name, m.SizeBytes, m.ModifiedAt)).ToList());
});

app.MapPost("/api/providers/{name}/execute", async (
    string name,
    ExecutionApiRequest request,
    IOptions<ProviderOptions> opts,
    IModelExecutionService executionService,
    CancellationToken cancellationToken) =>
{
    if (!opts.Value.Endpoints.TryGetValue(name, out var endpointOpts))
        return Results.NotFound();

    var endpoint = new ProviderEndpoint(name, endpointOpts.BaseUrl);
    var messages = request.Messages
        .Select(m => new ConversationMessage(m.Role, m.Content))
        .ToList();
    var executionRequest = new ExecutionRequest(request.ModelName, messages, request.Temperature, request.MaxTokens);
    var result = await executionService.ExecuteAsync(endpoint, executionRequest, cancellationToken);
    return Results.Ok(new ExecutionApiResponse(result.Content, result.ModelName, result.FinishReason));
});

app.MapGet("/api/profiles", (IBehaviorProfileService profileService) =>
{
    var items = profileService.GetAll().Select(p => new BehaviorProfileApiItem(
        p.Id, p.Name, p.Description, p.SystemPrompt,
        p.PreferredProviderName, p.PreferredModelName,
        p.Plugins.Select(b => new PluginBindingApiItem(b.Name, b.Tools)).ToList())).ToList();
    return Results.Ok(items);
});

app.MapGet("/api/mcp", (IOptions<McpOptions> opts) =>
{
    var servers = opts.Value.Servers
        .Select(kvp => new McpServerApiItem(kvp.Key, kvp.Value.Label, kvp.Value.Enabled))
        .ToList();
    return Results.Ok(servers);
});

app.MapPost("/api/mcp/{name}/availability", async (
    string name,
    IOptions<McpOptions> opts,
    IMcpAvailabilityService availabilityService,
    CancellationToken cancellationToken) =>
{
    if (!opts.Value.Servers.TryGetValue(name, out var serverOpts))
        return Results.NotFound();

    var server = new McpServerDefinition(
        name,
        serverOpts.Label,
        serverOpts.Transport,
        serverOpts.BaseUrl,
        serverOpts.Command,
        serverOpts.Args?.AsReadOnly(),
        serverOpts.Env as IReadOnlyDictionary<string, string>,
        serverOpts.Enabled);

    var result = await availabilityService.CheckAvailabilityAsync(server, cancellationToken);
    return Results.Ok(new McpAvailabilityApiResponse(result.Name, result.IsAvailable, result.ErrorMessage));
});

app.MapPost("/api/providers/{name}/agentic", async (
    string name,
    AgenticApiRequest request,
    IOptions<ProviderOptions> opts,
    IBehaviorProfileService profileService,
    IAgenticExecutionService agenticService,
    IPluginRegistry pluginRegistry,
    CancellationToken cancellationToken) =>
{
    if (!opts.Value.Endpoints.TryGetValue(name, out var endpointOpts))
        return Results.NotFound();

    var profile = profileService.GetById(request.ProfileId);
    if (profile is null)
        return Results.BadRequest($"Profile '{request.ProfileId}' not found.");

    var endpoint = new ProviderEndpoint(name, endpointOpts.BaseUrl);
    var plugins = pluginRegistry.Resolve(profile.Plugins);

    var agenticRequest = new AgenticRequest(
        request.ModelName,
        request.Prompt,
        plugins,
        request.MaxIterations ?? 10,
        SystemPrompt: profile.SystemPrompt);

    var result = await agenticService.ExecuteAsync(endpoint, agenticRequest, cancellationToken);
    return Results.Ok(new AgenticApiResponse(result.Content, result.FinishReason, result.ToolsInvoked));
});

app.Run();

public partial class Program { }

internal sealed record ProviderEndpointResponse(string Name, string BaseUrl);
internal sealed record ProviderStatusResponse(bool IsReachable, string? ErrorMessage);
internal sealed record ModelApiItem(string Name, long SizeBytes, DateTimeOffset ModifiedAt);
internal sealed record ExecutionApiRequest(
    string ModelName,
    IReadOnlyList<MessageApiItem> Messages,
    double? Temperature = null,
    int? MaxTokens = null);
internal sealed record MessageApiItem(MessageRole Role, string Content);
internal sealed record ExecutionApiResponse(string Content, string ModelName, FinishReason FinishReason);
internal sealed record McpServerApiItem(string Name, string Label, bool Enabled);
internal sealed record McpAvailabilityApiResponse(string Name, bool IsAvailable, string? ErrorMessage);
internal sealed record AgenticApiRequest(
    string ModelName,
    string Prompt,
    string ProfileId,
    int? MaxIterations = null);
internal sealed record AgenticApiResponse(string Content, AgenticFinishReason FinishReason, IReadOnlyList<string> ToolsInvoked);
internal sealed record PluginBindingApiItem(string Name, IReadOnlyList<string> Tools);
internal sealed record BehaviorProfileApiItem(
    string Id,
    string Name,
    string Description,
    string SystemPrompt,
    string? PreferredProviderName,
    string? PreferredModelName,
    IReadOnlyList<PluginBindingApiItem> Plugins);
