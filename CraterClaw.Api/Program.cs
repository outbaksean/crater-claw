using System.Text.Json.Serialization;
using CraterClaw.Api.Endpoints;
using CraterClaw.Api.Services;
using CraterClaw.Core;
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
builder.Services.AddSingleton<IProviderResolver, ProviderResolver>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

app.MapProviderEndpoints();
app.MapProfileEndpoints();

app.Run();

public partial class Program { }
