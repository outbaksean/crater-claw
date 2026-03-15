# CraterClaw Library MCP Configuration Spec

## Name
- CraterClaw Library MCP Configuration

## Purpose
- Define contracts and services for loading configured MCP server definitions and checking their availability on demand, without managing or deploying MCP servers.

## Scope
- Add MCP configuration contracts:
  - `McpTransport`: discriminated value (Stdio, Http)
  - `McpServerDefinition`: immutable record with name, display label, transport type, transport-specific connection parameters, and enabled flag
    - Stdio parameters: command path and argument list
    - Http parameters: base URL
  - `McpAvailabilityResult`: immutable record with server name, availability flag, and optional error message
  - `IMcpConfigurationService`: load and save MCP server definitions from a JSON file
  - `IMcpAvailabilityService`: check whether a given MCP server is reachable
- Define the JSON schema for MCP server configuration (separate file from provider-config.json).
- Implement availability checks:
  - Http: HTTP GET to the server base URL; reachable if a response is returned regardless of status code
  - Stdio: check that the command exists on the system PATH or as an absolute path; do not spawn the process
- Wire the console harness to list configured MCP servers and their enabled status, and to trigger an availability check for a selected server.
- Add automated tests for configuration parsing, validation, and availability service behavior without live MCP endpoints.

## Contract Notes
- Server names must be unique (case-insensitive) within a configuration file.
- Disabled servers may be listed and inspected but should not be included in agentic execution tool sets by default.
- Availability checks are explicit user-triggered operations, not automatic startup checks.
- `IMcpAvailabilityService` accepts a single `McpServerDefinition` so callers can check one server at a time without loading the full configuration again.
- Keep the schema minimal. Do not add tool definitions, capability negotiation, or protocol-level fields here. Those belong in the agentic execution spec.
- The MCP configuration file path is configurable at DI registration time, following the same pattern as `providerConfigurationPath` in `AddCraterClawCore`.

## JSON Schema (Outline)
```json
{
  "servers": [
    {
      "name": "searxng",
      "label": "SearXNG Web Search",
      "transport": "http",
      "baseUrl": "http://localhost:8080",
      "enabled": true
    },
    {
      "name": "obsidian",
      "label": "Obsidian Notes",
      "transport": "stdio",
      "command": "npx",
      "args": ["-y", "obsidian-mcp", "--vault", "C:/Users/seane/Documents/Notes"],
      "enabled": true
    }
  ]
}
```

## Prerequisites
- See [mcp-config-prereqs.md](mcp-config-prereqs.md) for required external services before implementing or manually verifying this spec.

## Manual Verification
- Dependencies: qBitTorrent MCP server running in SSE mode and reachable from this host (see mcp-config-prereqs.md).
- Create a config file with the qBitTorrent MCP server as an HTTP entry.
- Run the console harness and confirm the server appears in the list with correct transport type and enabled status.
- Trigger an availability check and confirm the result is displayed.

## Status
- Planning
