# CraterClaw Library MCP Configuration Plan

## Prerequisites
- See [mcp-config-prereqs.md](mcp-config-prereqs.md) for required external services before manual verification.

## Decisions

- `McpServerDefinition` is a flat sealed record with all transport-specific fields as nullable properties. Transport-specific fields that do not apply to the selected transport are validated away rather than using a discriminated union hierarchy, keeping the type simple to construct and serialize.
- Validation lives on a `McpConfiguration` wrapper record (mirroring `ProviderConfiguration`), not on `McpServerDefinition` directly, so the full list can be validated together for cross-record rules like uniqueness.
- The JSON transport value is stored as a lowercase string (`"http"`, `"stdio"`). The enum serializes/deserializes using a custom converter to avoid relying on numeric enum values.
- Http availability: HTTP GET to the server's base URL. Any HTTP response (including error codes) is treated as reachable; only a connection failure or timeout is unreachable. This avoids coupling the check to any specific MCP endpoint path.
- Stdio availability: walk `PATH` environment variable entries looking for the command file; also accept absolute paths directly. No process is spawned. `PATHEXT` extensions are checked on Windows. For `uvx`-based servers, this means checking that `uvx` is on PATH — the MCP package itself is downloaded by `uvx` on first invocation and is not checked here.
- `IMcpConfigurationService` is registered as Singleton (same pattern as `IProviderConfigurationService`). `IMcpAvailabilityService` is registered as Transient.
- The MCP config file path is passed to `AddCraterClawCore()` as a new optional parameter `mcpConfigurationPath`, defaulting to `./mcp-config.json`.

## Overview
- This plan implements the MCP configuration spec in two phases.
- Phase 1 defines contracts, validation, and tests. Phase 2 adds implementations, DI registration, and console wiring.

---

## Phase 1: Contracts, Validation, and Tests

### Status
- Not Started

### Goal
- Define all public contract types, validation rules, and automated tests that will drive the Phase 2 implementations.

### Contract
- `McpTransport` enum in `CraterClaw.Core`: `Http`, `Stdio`
- `McpServerDefinition` sealed record in `CraterClaw.Core`:
  - `Name` (string)
  - `Label` (string)
  - `Transport` (McpTransport)
  - `BaseUrl` (string?) — required when Transport is Http; must be a valid absolute URI
  - `Command` (string?) — required when Transport is Stdio
  - `Args` (IReadOnlyList<string>?) — optional, Stdio only
  - `Env` (IReadOnlyDictionary<string, string>?) — optional, Stdio only; passed as environment variables to the spawned process
  - `Enabled` (bool)
- `McpConfiguration` sealed record in `CraterClaw.Core`:
  - `Servers` (IReadOnlyList\<McpServerDefinition\>)
  - `IReadOnlyList<string> Validate()` — returns validation errors
  - `void ValidateOrThrow()` — throws `InvalidOperationException` if invalid
  - Validation rules:
    - Server names are unique (case-insensitive)
    - Each server name and label are non-empty
    - Http servers have a valid absolute `BaseUrl`
    - Stdio servers have a non-empty `Command`
- `McpAvailabilityResult` sealed record in `CraterClaw.Core`: `Name` (string), `IsAvailable` (bool), `ErrorMessage` (string?)
- `IMcpConfigurationService` interface in `CraterClaw.Core`:
  - `Task<McpConfiguration> LoadAsync(CancellationToken cancellationToken)`
  - `Task SaveAsync(McpConfiguration configuration, CancellationToken cancellationToken)`
- `IMcpAvailabilityService` interface in `CraterClaw.Core`:
  - `Task<McpAvailabilityResult> CheckAvailabilityAsync(McpServerDefinition server, CancellationToken cancellationToken)`

### Tasks
- Add `McpTransport.cs`, `McpServerDefinition.cs`, `McpConfiguration.cs`, `McpAvailabilityResult.cs`, `IMcpConfigurationService.cs`, `IMcpAvailabilityService.cs` in `CraterClaw.Core`.
- Add `McpConfigurationContractTests.cs` in `CraterClaw.Core.Tests`.
- Add `FileMcpConfigurationServiceTests.cs` in `CraterClaw.Core.Tests`.
- Add `McpAvailabilityServiceTests.cs` in `CraterClaw.Core.Tests`.

### Tests

`McpConfigurationContractTests`:
- Valid configuration with a Stdio server including `Env` entries passes validation.
- Duplicate server names (case-insensitive) produce a validation error.
- Http server with missing or malformed `BaseUrl` produces a validation error.
- Stdio server with empty `Command` produces a validation error.

`FileMcpConfigurationServiceTests`:
- Load returns correct `McpConfiguration` for valid JSON including `env` entries on a Stdio server.
- Save persists configuration and can be reloaded with consistent values including `env` entries.
- Load throws `InvalidOperationException` for malformed JSON.
- Load throws `FileNotFoundException` when file does not exist.

`McpAvailabilityServiceTests`:
- Http server returns `IsAvailable = true` when HTTP GET receives any response.
- Http server returns `IsAvailable = false` when connection is refused (HttpRequestException).
- Stdio server returns `IsAvailable = true` when command is found on PATH.
- Stdio server returns `IsAvailable = false` when command is not found on PATH or as an absolute path.
- Propagates cancellation when token is cancelled during Http check.

### Manual Verification
- `dotnet build CraterClaw.slnx` succeeds.
- `dotnet test CraterClaw.slnx --no-build` passes with all new tests included.

---

## Phase 2: Implementations, DI Registration, and Console Wiring

### Status
- Not Started

### Goal
- Implement `FileMcpConfigurationService` and `McpAvailabilityService`, register both in DI, and add a console flow to list servers and check availability.

### Contract
- No new public surface beyond Phase 1.
- JSON uses camelCase property names and a lowercase string for `transport` (`"http"` or `"stdio"`).
- Console lists servers numbered with label, transport type, and enabled status. Prompts for a selection by number to trigger an availability check and displays the result.

### Tasks
- Add `FileMcpConfigurationService.cs` (internal, sealed) in `CraterClaw.Core`:
  - Inject config file path via constructor.
  - Use `JsonNamingPolicy.CamelCase` and a `JsonStringEnumConverter` with lowercase naming for `McpTransport`.
  - Deserialize via private DTOs; validate on load; throw on invalid content.
  - Create parent directory on save if needed.
- Add `McpAvailabilityService.cs` (internal, sealed) in `CraterClaw.Core`:
  - Inject `HttpClient` via constructor.
  - Http check: GET `server.BaseUrl`; catch `HttpRequestException` as unavailable.
  - Stdio check: if `Command` is rooted, check `File.Exists`; otherwise walk `PATH` and `PATHEXT` entries.
- Update `ServiceCollectionExtensions.AddCraterClawCore()`:
  - Add `string? mcpConfigurationPath = null` parameter.
  - Register `IMcpConfigurationService` as Singleton → `FileMcpConfigurationService`.
  - Register `IMcpAvailabilityService` as Transient → `McpAvailabilityService`.
  - Default path: `./mcp-config.json`.
- Update `CraterClaw.Console/Program.cs`:
  - Accept an optional second argument for the MCP config path; prompt if not provided; default to `./mcp-config.json`.
  - After the execution flow, load MCP config and display servers numbered: `{n}. {label}  ({transport}, {enabled/disabled})`.
  - If no servers are configured, display `No MCP servers configured.` and skip.
  - Prompt `Select server number to check availability (leave blank to skip):`.
  - Display `Available: {name}` or `Unavailable: {name} — {error}`.

### Tests
- No new tests required; Phase 1 tests cover service behavior.

### Manual Verification Plan
- Prerequisites: `uv` installed on this machine; qBitTorrent WebUI reachable from this machine (see mcp-config-prereqs.md).
- Create `mcp-config.json` with the qBitTorrent server configured as a Stdio entry using `uvx` and the appropriate `env` values.
- Run the console harness and confirm the server appears in the numbered list with transport and enabled status.
- Select it and confirm the availability check detects `uvx` on PATH and reports available.

---

## Completion Criteria
- Both phase statuses are marked Done.
- `CraterClaw.Core` and `CraterClaw.Console` build successfully.
- All automated tests pass.
- Manual verification confirms availability check works against the live qBitTorrent MCP server.
- `mcp-config-spec.md` Status is updated to Done.
