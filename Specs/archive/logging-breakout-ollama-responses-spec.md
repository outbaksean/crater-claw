# Logging Breakout — Ollama Responses Spec

## Name
- Logging Breakout — Ollama Responses

## Checkpoint
- logging-breakout-ollama-responses

## Purpose
Separate AI traffic detail (full requests and responses to/from Ollama) from the main application log into a dedicated file. The AI log is toggled via config and uses a distinct Serilog sink. The main log retains only high-level lifecycle events. Both the console and API use the same logging setup.

## Design

### Log Categories

All AI traffic is logged under the fixed category `"CraterClaw.AiTraffic"` using a named logger created via `ILoggerFactory`. Serilog routes this category:
- Always excluded from the main log sink.
- Written to a dedicated rolling file only when AI logging is enabled.

### What Goes Where

**Main log (unchanged structure, kept):**
- Model execution started / finished with finish reason
- Agentic iteration count, tool invocations, finish reason
- Warnings and errors

**AI log (new, full content, no truncation):**
- `OllamaModelExecutionService`: full request JSON sent to `/api/chat`; full response message content received
- `SemanticKernelAgenticExecutionService`: full message history sent to the LLM each iteration (role + content, no truncation); full received message content and function call details

### Config

```json
"aiLogging": {
    "enabled": false,
    "path": ""
}
```

`path` is a file path prefix for the rolling log file. When empty, defaults to `logs/ai-` relative to the application base directory. When relative, resolved against `AppContext.BaseDirectory`.

---

## Phase 1: Core Service Changes

**Status: Done**

### Contract

**New type: `AiLoggingOptions`** in `CraterClaw.Core`:
```csharp
public sealed class AiLoggingOptions
{
    public bool Enabled { get; init; } = false;
    public string Path { get; init; } = string.Empty;
}
```
Bound to the `aiLogging` configuration section. No validator needed.

**`OllamaModelExecutionService`** — add `ILoggerFactory loggerFactory` as a constructor parameter. Create `private readonly ILogger _aiLogger = loggerFactory.CreateLogger("CraterClaw.AiTraffic")`.

Current AI-traffic log calls to move from `logger` to `_aiLogger`:
- The existing `logger.LogDebug("Request body: {RequestJson}", json)` line — move to `_aiLogger.LogDebug`.
- Add a new `_aiLogger.LogDebug("Response content: {Content}", response.Message?.Content)` after the response is deserialized successfully.

**`SemanticKernelAgenticExecutionService`** — add `ILoggerFactory loggerFactory` as a constructor parameter. Create `private readonly ILogger _aiLogger = loggerFactory.CreateLogger("CraterClaw.AiTraffic")`.

Current log calls to move from `logger` to `_aiLogger` (removing the 200-char truncation):

Before each LLM call (non-streaming path and streaming path), the per-message loop:
```csharp
// Before: used 'preview' truncated to 200 chars — replace with full content
_aiLogger.LogDebug("  [{Role}] content={Content} calls=[{Calls}] results=[{Results}]",
    msg.Role, msg.Content, functionCallSummary, functionResultSummary);
```

After the non-streaming LLM call, the received-message loop:
```csharp
// Before: used 'preview' truncated to 200 chars — replace with full content
_aiLogger.LogDebug("  [{Role}] content={Content} calls=[{Calls}]",
    msg.Role, msg.Content, functionCallSummary);
```

The iteration-count `LogDebug` lines (`"Iteration {n}: sending {count} messages"`, `"received {count} messages"`, `"stream complete, calls=[...]"`) move to `_aiLogger` unchanged.

The following stay on the main `logger` (no change):
- `LogInformation("Tool invoked: {Tool}", tool)`
- `LogInformation("Agentic task finished: {FinishReason}", finishReason)`

**`ServiceCollectionExtensions`** — register `AiLoggingOptions`:
```csharp
services.AddOptions<AiLoggingOptions>()
    .Bind(configuration.GetSection("aiLogging"));
```
No validator.

**`craterclaw.json`** — add section:
```json
"aiLogging": {
    "enabled": false,
    "path": ""
}
```

### Tests

All existing tests instantiate services with `NullLogger<T>.Instance` directly. After this change both services also require `ILoggerFactory`. Update every direct constructor call to pass `NullLoggerFactory.Instance` as the final argument.

Affected helpers:
- `OllamaModelExecutionServiceTests.CreateClient` factory methods — update `new OllamaModelExecutionService(client, NullLogger<...>.Instance)` to add `NullLoggerFactory.Instance`.
- `SemanticKernelAgenticExecutionServiceTests.BuildService` — update `new SemanticKernelAgenticExecutionService(new FakeKernelFactory(kernel), NullLogger<...>.Instance)` to add `NullLoggerFactory.Instance`.
- `SemanticKernelAgenticExecutionServiceTests` test that manually constructs the service — same update.

Add the following new tests to `OllamaModelExecutionServiceTests`:

1. `ExecuteAsync_LogsRequestJsonToAiLogger`
   Use a `CapturingLoggerFactory`. Execute with a valid response. Assert the AI logger received a message containing the serialized model name and `"messages"`.

2. `ExecuteAsync_LogsResponseContentToAiLogger`
   Use a `CapturingLoggerFactory`. Execute with a response where `message.content` is `"test response"`. Assert the AI logger received a message containing `"test response"`.

Add `CapturingLoggerFactory` as a private nested class in `OllamaModelExecutionServiceTests`:
```csharp
private sealed class CapturingLoggerFactory : ILoggerFactory
{
    public List<string> Messages { get; } = [];
    public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);
    public void AddProvider(ILoggerProvider provider) { }
    public void Dispose() { }

    private sealed class CapturingLogger(List<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
            => messages.Add(formatter(state, exception));
    }
}
```

### README Sync

No README changes in this phase.

### Current Architecture Sync

No current-architecture changes in this phase.

### Manual Verification Plan

Not applicable to this phase — no visible output change yet. Proceed to Phase 2.

---

## Phase 2: Serilog Routing in Console and API

**Status: Done**

### Contract

**Console (`CraterClaw.Console/Program.cs`):**

Replace the current flat Serilog configuration:
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

With a sub-logger configuration that separates AI traffic:
```csharp
var aiEnabled = configuration.GetValue<bool>("aiLogging:enabled");
var aiPathConfig = configuration.GetValue<string>("aiLogging:path") ?? string.Empty;
var aiLogPath = string.IsNullOrWhiteSpace(aiPathConfig)
    ? Path.Combine(logDirectory, "ai-")
    : Path.IsPathRooted(aiPathConfig)
        ? aiPathConfig
        : Path.Combine(AppContext.BaseDirectory, aiPathConfig);

var logConfig = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Logger(lc => lc
        .Filter.ByExcluding(e =>
            e.Properties.TryGetValue("SourceContext", out var sc) &&
            sc.ToString().Trim('"') == "CraterClaw.AiTraffic")
        .WriteTo.File(logPath, rollingInterval: RollingInterval.Day));

if (aiEnabled)
{
    logConfig = logConfig.WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e =>
            e.Properties.TryGetValue("SourceContext", out var sc) &&
            sc.ToString().Trim('"') == "CraterClaw.AiTraffic")
        .WriteTo.File(aiLogPath, rollingInterval: RollingInterval.Day));
}

Log.Logger = logConfig.CreateLogger();
```

**API (`CraterClaw.Api/CraterClaw.Api.csproj`):**

Add packages:
```xml
<PackageReference Include="Serilog" Version="4.2.0" />
<PackageReference Include="Serilog.Extensions.Hosting" Version="9.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="6.0.0" />
```

**API (`CraterClaw.Api/Program.cs`):**

After the configuration sources are set up and before `builder.Services.AddCraterClawCore`, add Serilog:

```csharp
var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
var logPath = Path.Combine(logDirectory, "craterclaw-.log");

var aiEnabled = builder.Configuration.GetValue<bool>("aiLogging:enabled");
var aiPathConfig = builder.Configuration.GetValue<string>("aiLogging:path") ?? string.Empty;
var aiLogPath = string.IsNullOrWhiteSpace(aiPathConfig)
    ? Path.Combine(logDirectory, "ai-")
    : Path.IsPathRooted(aiPathConfig)
        ? aiPathConfig
        : Path.Combine(AppContext.BaseDirectory, aiPathConfig);

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
{
    logConfig = logConfig.WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e =>
            e.Properties.TryGetValue("SourceContext", out var sc) &&
            sc.ToString().Trim('"') == "CraterClaw.AiTraffic")
        .WriteTo.File(aiLogPath, rollingInterval: RollingInterval.Day));
}

builder.Host.UseSerilog(logConfig.CreateLogger());
```

Note: `MinimumLevel.Override` for `Microsoft` and `System` prevents ASP.NET Core's own verbose debug logs from flooding the main log file.

### Tests

No test changes in this phase. Existing tests are not affected by entry-point configuration.

### README Sync

In the Configuration section, add a `### AI logging` subsection after the qBitTorrent section:

Describe:
- `aiLogging.enabled` (bool, default `false`) — when true, Ollama request/response detail is written to a separate rolling log file.
- `aiLogging.path` (string, optional) — file path prefix for the AI log. When empty, defaults to `logs/ai-` relative to the application directory. Can be absolute or relative to the app directory.
- How to enable via user secrets: `dotnet user-secrets set "aiLogging:enabled" "true" --project .\CraterClaw.Console`.

Also note that the console now prints the AI log path (if enabled) alongside the main log path on startup.

Update the console startup output section: add that `Log file: {logDirectory}` prints at startup and, if AI logging is enabled, `AI log file: {aiLogDirectory}` is also printed.

### Current Architecture Sync

Update `current-architecture.md`:
- Under Logging: note that `CraterClaw.AiTraffic` is a separate Serilog category carrying full Ollama request/response detail, routed to a separate file when `aiLogging.enabled` is true.
- Note that `AiLoggingOptions` is bound to the `aiLogging` config section.
- Note the API now uses Serilog (same configuration as the console) with `MinimumLevel.Override` for Microsoft/System namespaces.

### Manual Verification Plan

Prerequisites: console or API running with Ollama reachable.

1. With `aiLogging.enabled = false` (default): run the console through a full agentic task. Open the main log file and confirm it contains tool invocations and finish reason but no message content or request JSON.

2. Set `aiLogging.enabled = true` via user secrets. Run an agentic task. Confirm:
   - The main log still has no message content.
   - A new AI log file exists in the `logs/` directory.
   - The AI log contains the full request JSON including the model name and messages array.
   - The AI log contains the full response content.
   - For multi-iteration tasks, the AI log shows the full chat history sent on each iteration without truncation.

3. Run the API with `aiLogging.enabled = true`. Issue a `POST /api/providers/{name}/agentic` request. Confirm the AI log captures the same detail as the console.
