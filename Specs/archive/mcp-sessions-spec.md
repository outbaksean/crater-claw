# CraterClaw Library MCP Sessions Spec

## Name
- CraterClaw Library MCP Sessions

## Purpose
- Add the NuGet packages and internal wiring needed to create Semantic Kernel MCP clients from configured `McpServerDefinition` records, and update the console to list available tools from permitted servers after profile selection.

## Scope
- Add NuGet packages to `CraterClaw.Core`:
  - `Microsoft.SemanticKernel` — core kernel and plugin types
  - `Microsoft.SemanticKernel.Connectors.Ollama` — Ollama chat completion connector (alpha)
  - `ModelContextProtocol` — MCP client SDK (`McpClientFactory`, `StdioClientTransport`, `SseClientTransport`)
- Add an internal helper `McpClientFactory` wrapper (or extension method) in `CraterClaw.Core` that creates an `IMcpClient` from a `McpServerDefinition`:
  - `Transport = Stdio`: uses `StdioClientTransport` with `Command`, `Args`, and `Env` from the definition
  - `Transport = Http`: uses `SseClientTransport` (Streamable HTTP) with `BaseUrl` from the definition
  - Throws `InvalidOperationException` for unsupported transports
- Update the console harness: after profile selection, if a profile was selected:
  - Resolve permitted MCP servers: filter `McpOptions.Servers` to those whose name appears in the profile's `AllowedMcpServerNames` and whose `Enabled` flag is true
  - For each resolved server, create an `IMcpClient`, call `ListToolsAsync()`, and display the tools
  - Console output format per server: `{ServerLabel} tools ({n}):` followed by numbered entries `{n}. {toolName} - {toolDescription}`
  - If a server has no tools or fails to connect, display a warning and continue
  - Dispose all clients after the listing is complete
- Add automated tests for the `McpServerDefinition`-to-transport mapping logic.

## Contract Notes
- `IMcpClient` is from the `ModelContextProtocol` SDK and is not a CraterClaw-owned type. CraterClaw code references it directly where needed.
- Environment variables in `McpServerDefinition.Env` are merged with the current process environment for Stdio transports.
- This spec does not introduce any new public contract types in `CraterClaw.Core`. The SK and MCP packages are added here but their types are used internally.
- Tool listing in the console is a diagnostic display only — it does not affect the agentic task run defined in the execution-loop spec.

## Manual Verification
- Dependencies: `uv` installed; qBitTorrent MCP server startable via `uvx`; `qbittorrent-manager` profile configured in `craterclaw.json`.
- Run the console harness and select the `qbittorrent-manager` profile.
- Confirm the console connects to the qBitTorrent MCP server and displays its available tools by name and description.

## Status
- Done
