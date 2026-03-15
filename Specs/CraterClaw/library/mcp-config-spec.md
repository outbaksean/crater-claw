# CraterClaw Library MCP Configuration Spec

## Name
- CraterClaw Library MCP Configuration

## Purpose
- Define contracts and services for loading configured MCP server definitions and checking their availability on demand, without managing or deploying MCP servers.

## Scope
- Add MCP configuration contracts:
  - `McpTransport`: discriminated value (Stdio, Http)
  - `McpServerDefinition`: immutable record with name, display label, transport type, transport-specific connection parameters, optional environment variables, and enabled flag
    - Stdio parameters: command path, argument list, and optional environment variable map passed to the spawned process
    - Http parameters: base URL
  - `McpAvailabilityResult`: immutable record with server name, availability flag, and optional error message
  - `IMcpConfigurationService`: load and save MCP server definitions from a JSON file
  - `IMcpAvailabilityService`: check whether a given MCP server is reachable
- Define the JSON schema for MCP server configuration (separate file from provider-config.json).
- Implement availability checks:
  - Http: HTTP GET to the server base URL; reachable if a response is returned regardless of status code
  - Stdio: check that the command exists on the system PATH or as an absolute path; do not spawn the process. For `uvx`-based servers this means checking that `uvx` is installed, not that the MCP package has been downloaded.
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
      "name": "qbittorrent",
      "label": "qBitTorrent",
      "transport": "stdio",
      "command": "uvx",
      "args": ["--from", "git+https://github.com/jmagar/yarr-mcp", "qbittorrent-mcp-server"],
      "env": {
        "QBITTORRENT_URL": "http://192.168.1.x:8080",
        "QBITTORRENT_USER": "admin",
        "QBITTORRENT_PASS": "password",
        "QBITTORRENT_MCP_TRANSPORT": "stdio"
      },
      "enabled": true
    }
  ]
}
```

## Prerequisites
- See [mcp-config-prereqs.md](mcp-config-prereqs.md) for required external services before implementing or manually verifying this spec.

## Manual Verification
- Dependencies: `uv` installed on this machine; qBitTorrent WebUI reachable from this machine (see mcp-config-prereqs.md).
- Create `mcp-config.json` with the qBitTorrent server configured as a Stdio entry using `uvx` (see JSON schema above).
- Run the console harness and confirm the server appears in the numbered list with correct transport type and enabled status.
- Trigger an availability check and confirm `uvx` is detected as available.

## Status
- Done
