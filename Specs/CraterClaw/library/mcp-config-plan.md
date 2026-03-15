# CraterClaw Library MCP Configuration Plan

## Prerequisites
- See [mcp-config-prereqs.md](mcp-config-prereqs.md) for required external services before manual verification.

## Decisions

- MCP configuration uses `IOptions<McpOptions>` bound from the `mcp` section of `craterclaw.json`, consistent with the secrets spec. `IMcpConfigurationService` and `FileMcpConfigurationService` are not implemented.
- The `mcp.servers` sub-key holds a dictionary of `McpServerOptions` POCOs keyed by server name. This keeps user secret paths stable (`mcp:servers:qbittorrent:env:QBITTORRENT_PASS`).
- Sensitive env values (passwords, URLs) are left as empty strings in `craterclaw.json`. User secrets or OS environment variables supply real values at runtime.
- `McpServerDefinition` is an immutable sealed record used at runtime. It is constructed from `McpServerOptions` + dictionary key (name) at the call site.
- `McpOptionsValidator` validates transport-specific fields (Http requires valid BaseUrl; Stdio requires non-empty Command). An empty `Servers` dictionary is valid — MCP is optional.
- Http availability: GET the server's BaseUrl; any response is treated as available. Only `HttpRequestException` is unreachable.
- Stdio availability: if Command is rooted, check `File.Exists`. Otherwise walk `PATH` entries (with `PATHEXT` extensions on Windows). No process is spawned.
- `IMcpAvailabilityService` is registered as Transient. No singleton configuration service is needed since options are injected directly.
- The `McpTransport` enum value is bound from a string in JSON (`"Stdio"`, `"Http"`). IConfiguration's enum binding is case-insensitive.

## Overview
- Phase 1 defines contract types, options POCOs, validation, and tests.
- Phase 2 implements `McpAvailabilityService`, updates DI and console wiring, and adds the `mcp` section to `craterclaw.json`.

---

## Phase 1: Contracts, Options Types, Validation, and Tests

### Status
- Done

### Goal
- Define all public contract types, options POCOs, the validator, and tests.

### Contract
- `McpTransport` enum in `CraterClaw.Core`: `Http`, `Stdio`
- `McpServerDefinition` sealed record in `CraterClaw.Core` (runtime type):
  - `Name` (string), `Label` (string), `Transport` (McpTransport), `BaseUrl` (string?), `Command` (string?), `Args` (IReadOnlyList<string>?), `Env` (IReadOnlyDictionary<string, string>?), `Enabled` (bool)
- `McpAvailabilityResult` sealed record in `CraterClaw.Core`: `Name` (string), `IsAvailable` (bool), `ErrorMessage` (string?)
- `IMcpAvailabilityService` interface in `CraterClaw.Core`:
  - `Task<McpAvailabilityResult> CheckAvailabilityAsync(McpServerDefinition server, CancellationToken cancellationToken)`
- `McpServerOptions` sealed class in `CraterClaw.Core` (mutable, for IConfiguration binding):
  - `Label` (string), `Transport` (McpTransport), `BaseUrl` (string?), `Command` (string?), `Args` (List<string>?), `Env` (Dictionary<string, string>?), `Enabled` (bool)
- `McpOptions` sealed class in `CraterClaw.Core` (mutable, for IConfiguration binding):
  - `Servers` (Dictionary<string, McpServerOptions>)
- `McpOptionsValidator` (internal, sealed) implementing `IValidateOptions<McpOptions>`:
  - Http servers require a valid absolute BaseUrl
  - Stdio servers require a non-empty Command
  - Each server must have a non-empty Label
  - Empty Servers dictionary passes validation

### Tasks
- Add `McpTransport.cs`, `McpServerDefinition.cs`, `McpAvailabilityResult.cs`, `IMcpAvailabilityService.cs` in `CraterClaw.Core`.
- Add `McpServerOptions.cs`, `McpOptions.cs`, `McpOptionsValidator.cs` in `CraterClaw.Core`.
- Add `McpOptionsValidatorTests.cs` and `McpAvailabilityServiceTests.cs` in `CraterClaw.Core.Tests`.

### Tests

`McpOptionsValidatorTests`:
- Valid Stdio server with env entries passes validation.
- Valid Http server with absolute BaseUrl passes validation.
- Http server with invalid BaseUrl fails validation.
- Stdio server with empty Command fails validation.
- Empty Servers dictionary passes validation.

`McpAvailabilityServiceTests`:
- Http server returns `IsAvailable = true` when HTTP GET receives any response.
- Http server returns `IsAvailable = false` when connection fails (HttpRequestException).
- Propagates cancellation during Http check.
- Stdio server returns `IsAvailable = true` when command is found on PATH.
- Stdio server returns `IsAvailable = false` when command is not found.

---

## Phase 2: Implementation, DI Registration, and Console Wiring

### Status
- Done

### Goal
- Implement `McpAvailabilityService`, register in DI, add `mcp` section to `craterclaw.json`, update console.

### Contract
- No new public surface beyond Phase 1.
- Console lists MCP servers numbered: `{n}. {label}  ({transport}, {enabled/disabled})`.
- Prompts for selection to trigger availability check and displays result.

### Tasks
- Add `McpAvailabilityService.cs` (internal, sealed) in `CraterClaw.Core`.
- Update `ServiceCollectionExtensions.AddCraterClawCore()` to register `McpOptions` and `IMcpAvailabilityService`.
- Add `mcp.servers` section to `craterclaw.json` with placeholder env values.
- Update `CraterClaw.Console/Program.cs` to display MCP servers and run availability checks.

### Tests
- No new tests required; Phase 1 covers all service behavior.

### Manual Verification Plan
- Prerequisites: `uv` installed on this machine (see mcp-config-prereqs.md).
- Run the console and confirm the qBitTorrent server appears in the MCP list.
- Select it and confirm the availability check detects `uvx` on PATH and reports available.
- Set `dotnet user-secrets set "mcp:servers:qbittorrent:env:QBITTORRENT_URL" "http://..."` and confirm it appears in the options value at runtime (visible via a debug breakpoint or future logging).

---

## Completion Criteria
- Both phase statuses are marked Done.
- All automated tests pass.
- Manual verification confirms availability check works.
- `mcp-config-spec.md` Status is updated to Done.
