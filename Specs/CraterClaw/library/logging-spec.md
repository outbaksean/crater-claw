# CraterClaw Library Logging Spec

## Name
- CraterClaw Library Logging

## Purpose
- Establish a consistent, structured logging foundation across all core services so that internal behavior — especially during agentic execution — is observable without polluting the interactive console output.

## Scope
- Register `Microsoft.Extensions.Logging` (MEL) in `AddCraterClawCore()` so all services can receive `ILogger<T>` via DI.
- Inject `ILogger<T>` into all existing core services and add log statements at appropriate levels.
- Wire a rolling file sink in the console harness so log output is written to a file rather than the interactive console.
- Define and document the log level convention used across the project.
- Do not add a logging UI, log viewer, or structured query surface. Do not introduce a third-party logging framework into the library itself — only into the console harness bootstrap.

## Log Level Convention
- `Debug` — request and response bodies, tool arguments and results, low-level HTTP details
- `Information` — significant state transitions: endpoint selected, model selected, loop iteration started/completed, tool called, finish reason
- `Warning` — recoverable errors: tool invocation failed but loop continues, model returned unexpected finish reason
- `Error` — unrecoverable failures that result in an exception being surfaced to the caller

## Services to Instrument
- `OllamaProviderStatusService`: log reachability check result at Information; log HTTP errors at Warning.
- `OllamaModelListingService`: log model count at Information; log HTTP errors at Warning.
- `OllamaModelExecutionService`: log model name and finish reason at Information; log request body at Debug; log HTTP and parse errors at Error.
- `McpAvailabilityService`: log availability result and transport type at Information; log failures at Warning.

## Console Harness Wiring
- Configure a rolling file sink writing to `logs/craterclaw-.log` (date-stamped) in the application base directory.
- Minimum log level for the file sink: `Debug`.
- Do not add a console log sink — all user-facing output remains explicit `Console.WriteLine` calls.
- The log file path should be visible at startup: print `Log file: {path}` before the first interactive prompt.

## Contract Notes
- The library registers the `ILoggerFactory` and `ILogger<T>` abstractions only. The sink is the console harness's responsibility.
- Automated tests do not need to assert on log output. Pass a `NullLogger<T>` or `NullLoggerFactory` in test constructors where needed.
- Do not log sensitive values (passwords, secret env vars) at any level.

## Manual Verification
- Run the console harness and confirm the log file path is printed at startup.
- Complete a full run (endpoint check, model list, prompt execution).
- Open the log file and confirm Information-level entries appear for each major step.
- Confirm no log output appears in the interactive console during the run.

## Status
- Done
