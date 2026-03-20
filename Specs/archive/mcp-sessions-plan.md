# CraterClaw Library MCP Sessions Plan

## Decisions

- Package versions should be confirmed at implementation time using `dotnet package search` or NuGet.org. Target the latest stable SK release and the latest `ModelContextProtocol` release.
- `IMcpClientProvider` is a public interface in `CraterClaw.Core` returning `McpClient` (the SDK's abstract class — no `IMcpClient` interface exists). It is consumed by the console and later by `SemanticKernelAgenticExecutionService`.
- `McpClientProvider` (internal, sealed) exposes `CreateTransport(McpServerDefinition)` as an `internal static` method. This is testable via `InternalsVisibleTo` without real process/network calls. No separate `ITransportFactory` abstraction is needed.
- HTTP transport is `HttpClientTransport` (not `SseClientTransport` — that class does not exist in v1.1.0). Configured via `HttpClientTransportOptions { Endpoint = new Uri(baseUrl) }`.
- Environment variables in `McpServerDefinition.Env` are merged with the current process environment for Stdio transports via `StdioClientTransportOptions.EnvironmentVariables`. They do not replace the environment entirely.
- MCP client options supply a fixed `clientInfo` of `{ name: "CraterClaw", version: "1.0" }`.
- The console disposes all `IMcpClient` instances after tool listing completes, regardless of success or failure per server.
- If a server fails to connect or list tools, a warning is printed and listing continues for remaining servers. The console does not exit.

## Overview
- Phase 1 adds NuGet packages, `IMcpClientProvider`, the internal transport factory abstraction, and tests.
- Phase 2 wires the console tool listing after profile selection.

---

## Phase 1: Packages, IMcpClientProvider, and Tests

### Status
- Done

### Goal
- Add SK and MCP packages, implement `IMcpClientProvider` with testable transport mapping, and register in DI.

### Contract

- `IMcpClientProvider` interface (public) in `CraterClaw.Core`:
  ```csharp
  public interface IMcpClientProvider
  {
      Task<IMcpClient> CreateClientAsync(
          McpServerDefinition server,
          CancellationToken cancellationToken);
  }
  ```
- `ITransportFactory` interface (internal) in `CraterClaw.Core`:
  ```csharp
  internal interface ITransportFactory
  {
      Task<IMcpClient> CreateClientAsync(
          IClientTransport transport,
          McpClientOptions options,
          CancellationToken cancellationToken);
  }
  ```
  Default implementation wraps `McpClientFactory.CreateAsync(transport, options, cancellationToken)`.

### Packages to Add to `CraterClaw.Core.csproj`
- `Microsoft.SemanticKernel` — confirm latest stable version
- `Microsoft.SemanticKernel.Connectors.Ollama` — confirm latest alpha version
- `ModelContextProtocol` — confirm latest stable version

### Tasks

**`CraterClaw.Core`**
- Add package references to `CraterClaw.Core.csproj`.
- Add `IMcpClientProvider.cs` (public interface).
- Add `ITransportFactory.cs` (internal interface) and `DefaultTransportFactory.cs` (internal, sealed) wrapping `McpClientFactory.CreateAsync`.
- Add `McpClientProvider.cs` (internal, sealed) implementing `IMcpClientProvider`:
  - Constructor: `ITransportFactory transportFactory`
  - Private method `CreateTransport(McpServerDefinition server) -> IClientTransport`:
    - `McpTransport.Stdio`: returns `new StdioClientTransport(new StdioClientTransportOptions { Command = server.Command, Arguments = server.Args, EnvironmentVariables = MergeEnv(server.Env) })`
    - `McpTransport.Http`: returns `new SseClientTransport(new SseClientTransportOptions { Endpoint = new Uri(server.BaseUrl!) })`
    - Other: throws `InvalidOperationException($"Unsupported transport: {server.Transport}")`
  - `CreateClientAsync`: calls `CreateTransport`, then `_transportFactory.CreateClientAsync(transport, new McpClientOptions { ClientInfo = new() { Name = "CraterClaw", Version = "1.0" } }, cancellationToken)`
  - Private static `MergeEnv`: merges `server.Env` into current `Environment.GetEnvironmentVariables()` as a `Dictionary<string, string>`
- Register in `ServiceCollectionExtensions.AddCraterClawCore()`:
  - `services.AddTransient<ITransportFactory, DefaultTransportFactory>()`
  - `services.AddTransient<IMcpClientProvider, McpClientProvider>()`

**`CraterClaw.Core.Tests`**
- Add `McpClientProviderTests.cs`.

### Tests

`McpClientProviderTests`:
- Stdio definition with valid Command creates a client without throwing (fake `ITransportFactory` returns a mock `IMcpClient`).
- Http definition with valid BaseUrl creates a client without throwing.
- Unsupported transport throws `InvalidOperationException`.
- Stdio transport is configured with the correct `Command` from the definition.
- Http transport is configured with the correct `BaseUrl` from the definition.
- `MergeEnv` includes both existing environment variables and definition-supplied `Env` entries; definition values take precedence on collision.

> Note: exact property names on `StdioClientTransportOptions` and `SseClientTransportOptions` should be confirmed against the SDK at implementation time and adjusted accordingly.

### Manual Verification
- `dotnet build CraterClaw.slnx` succeeds.
- `dotnet test CraterClaw.slnx --no-build` passes with new tests included.

---

## Phase 2: Console Tool Listing

### Status
- Done

### Goal
- After profile selection in the console, open MCP clients for permitted servers and display their tools.

### Contract
- No new public surface.
- Console output format:
  ```
  {ServerLabel} tools ({n}):
  1. {toolName} - {toolDescription}
  2. ...
  ```
- If a server fails to connect or list tools:
  ```
  Warning: could not list tools for {ServerLabel}: {errorMessage}
  ```

### Tasks

**`CraterClaw.Console/Program.cs`**
- Resolve `IMcpClientProvider` from the service provider.
- Replace the current profile selection stub (the `_ = selectedProfileId` block) with:
  - Resolve permitted servers: `mcpOptions.Servers` entries whose key is in `selectedProfile.AllowedMcpServerNames` and `Enabled == true`, constructed as `McpServerDefinition` records.
  - For each permitted server, in a `try/finally`:
    - Call `mcpClientProvider.CreateClientAsync(server, CancellationToken.None)`
    - Call `client.ListToolsAsync(cancellationToken: CancellationToken.None)`
    - Print the tool list in the specified format
    - Dispose the client in `finally`
  - Catch exceptions per server, print the warning, and continue.

### Tests
- No new tests required.

### Manual Verification Plan
- Prerequisites: `uv` installed on PATH; qBitTorrent MCP server startable via `uvx`.
- Run the console harness and select the `qbittorrent-manager` profile.
- Confirm the console opens a session to the qBitTorrent MCP server and displays the available tools with names and descriptions.
- Select the `no-tools` profile and confirm no tool listing occurs.

---

## Completion Criteria
- Both phase statuses are marked Done.
- All automated tests pass.
- Manual verification confirms tool listing works for the qBitTorrent profile.
- `mcp-sessions-spec.md` Status is updated to Done.
