# CraterClaw Library Logging Plan

## Decisions

- `AddLogging()` is called inside `AddCraterClawCore()` so `ILogger<T>` is resolvable regardless of whether the host configures a sink. The call is idempotent and safe.
- Each service receives `ILogger<T>` as a constructor parameter alongside its existing parameters. Primary constructor syntax is used, consistent with existing services.
- Tests pass `NullLogger<T>.Instance` (from `Microsoft.Extensions.Logging.Abstractions`) where a logger is now required. No test logic changes — the logger is a silent no-op.
- The rolling file sink uses Serilog (`Serilog`, `Serilog.Extensions.Logging`, `Serilog.Sinks.File`) added only to `CraterClaw.Console`. The library takes no Serilog dependency.
- Log file path: `{AppContext.BaseDirectory}/logs/craterclaw-{Date}.log`, one file per day, rolling daily.
- The log file path is printed to the console before the first interactive prompt.
- Sensitive values (passwords, env var contents) are never passed to log calls.

## Overview
- Phase 1 adds `ILogger<T>` to all core services, calls `AddLogging()` in DI registration, and updates test constructors.
- Phase 2 adds the Serilog file sink to the console harness and prints the log file path at startup.

---

## Phase 1: Instrument Core Services

### Status
- Done

### Goal
- Add structured log calls to all four core services. Wire `AddLogging()` in `AddCraterClawCore()`. Update tests to supply `NullLogger<T>.Instance`.

### Tasks

**`CraterClaw.Core`**
- Call `services.AddLogging()` in `ServiceCollectionExtensions.AddCraterClawCore()`.
- Update `OllamaProviderStatusService` — add `ILogger<OllamaProviderStatusService>` constructor parameter and log:
  - `Information` "Checking provider status for endpoint {EndpointName} at {BaseUrl}"
  - `Information` "Endpoint {EndpointName} is reachable" on success
  - `Warning` "Endpoint {EndpointName} is unreachable: {ErrorMessage}" on failure
- Update `OllamaModelListingService` — add `ILogger<OllamaModelListingService>` constructor parameter and log:
  - `Information` "Listing models for endpoint {EndpointName}"
  - `Information` "Found {ModelCount} model(s) for endpoint {EndpointName}"
  - `Warning` "Model listing failed for endpoint {EndpointName}: {ErrorMessage}" in catch blocks
- Update `OllamaModelExecutionService` — add `ILogger<OllamaModelExecutionService>` constructor parameter and log:
  - `Information` "Executing model {ModelName} at endpoint {EndpointName}"
  - `Debug` "Request body: {RequestJson}" after serialization
  - `Information` "Model {ModelName} finished with reason {FinishReason}"
  - `Error` "Model execution failed: {ErrorMessage}" in catch blocks
- Update `McpAvailabilityService` — add `ILogger<McpAvailabilityService>` constructor parameter and log:
  - `Information` "Checking availability of MCP server {ServerName} ({Transport})"
  - `Information` "MCP server {ServerName} is available"
  - `Warning` "MCP server {ServerName} is unavailable: {ErrorMessage}"

**`CraterClaw.Core.Tests`**
- Update all test constructors and factory helpers that directly instantiate the four services above to pass `NullLogger<T>.Instance` as the logger argument.

### Manual Verification
- `dotnet build CraterClaw.slnx` succeeds.
- `dotnet test CraterClaw.slnx --no-build` passes — all existing tests pass unchanged.

---

## Phase 2: Console Harness File Sink

### Status
- Done

### Goal
- Add Serilog packages to the console project, configure a rolling file sink, and print the log file path at startup.

### Tasks

**`CraterClaw.Console.csproj`**
- Add package references:
  - `Serilog`
  - `Serilog.Extensions.Logging`
  - `Serilog.Sinks.File`

**`CraterClaw.Console/Program.cs`**
- Before building the `ServiceCollection`, configure Serilog:
  ```csharp
  var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
  var logPath = Path.Combine(logDirectory, "craterclaw-.log");

  Log.Logger = new LoggerConfiguration()
      .MinimumLevel.Debug()
      .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
      .CreateLogger();
  ```
- After building the `ServiceCollection` (before `AddCraterClawCore`), add:
  ```csharp
  services.AddLogging(b => b.AddSerilog(dispose: true));
  ```
- Print the resolved log file path immediately before the first interactive prompt:
  ```
  Log file: {logDirectory}
  ```
- Wrap the application in a `try/finally` block (or use `Log.CloseAndFlush()` at exit) to ensure buffered log entries are flushed before the process exits.

### Tests
- No new tests required.

### Manual Verification Plan
- No external dependencies.
- Run the console harness and confirm `Log file: ...` is printed at startup.
- Complete a full run: select an endpoint, list models, run a prompt.
- Open the log file in the `logs/` directory and confirm:
  - Information entries appear for the status check, model listing, and model execution.
  - A Debug entry appears containing the request JSON.
  - No log output appeared in the interactive console during the run.

---

## Completion Criteria
- Both phase statuses are marked Done.
- All automated tests pass.
- Manual verification confirms log file is written and console output is clean.
- `logging-spec.md` Status is updated to Done.
