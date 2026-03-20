# MCP Configuration Prerequisites

## Required on the CraterClaw Machine

### uv
- `uv` must be installed and on the system PATH.
- CraterClaw spawns MCP servers as child processes using `uvx`, which is included with `uv`.
- `uvx` downloads and runs MCP packages on first invocation; no separate MCP installation step is required.
- See `qbittorrent-mcp-setup.md` in the project root for installation instructions.

## Required External Services

### qBitTorrent
- A qBitTorrent instance must be running with the WebUI enabled.
- The qBitTorrent WebUI must be reachable from the CraterClaw machine over the network.
- The WebUI URL, username, and password are supplied via the `env` block in `mcp-config.json` and are never hardcoded in the application.
