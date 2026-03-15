# MCP Configuration Prerequisites

## Required External Services

### qBitTorrent MCP Server
- A qBitTorrent MCP server must be running and reachable from the CraterClaw host before implementing or manually verifying the mcp-config spec.
- The server is an external dependency managed outside this repository.
- See `qbittorrent-mcp-setup.md` in the project root for installation and configuration instructions.
- The server must be running in SSE transport mode and accessible over HTTP from the CraterClaw host.
- The base URL of the running server (e.g., `http://192.168.1.x:8000`) is needed to populate `mcp-config.json`.

## qBitTorrent Instance
- A qBitTorrent instance must be running with the WebUI enabled and credentials configured.
- The qBitTorrent WebUI does not need to be directly reachable from the CraterClaw host; only the MCP server needs access to it.
