# qBitTorrent MCP Setup

CraterClaw manages the qBitTorrent MCP server as a child process using `uvx`. There is no separate server to install or keep running — `uvx` downloads and launches the MCP package automatically when CraterClaw needs it.

The MCP server is part of the [yarr-mcp](https://github.com/jmagar/yarr-mcp) project and is not part of this repository.

## Prerequisites

- `uv` installed on the CraterClaw machine (includes `uvx`)
- qBitTorrent running with the WebUI enabled and reachable from the CraterClaw machine

## Install uv

Follow the instructions at https://docs.astral.sh/uv/getting-started/installation/ for your platform.

Verify the installation:

```bash
uvx --version
```

## Configure mcp-config.json

Create `mcp-config.json` alongside `provider-config.json` (or at the path passed to the console harness) with the following content:

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
        "QBITTORRENT_PASS": "your-password",
        "QBITTORRENT_MCP_TRANSPORT": "stdio"
      },
      "enabled": true
    }
  ]
}
```

Replace `192.168.1.x` and the credentials with the actual qBitTorrent WebUI address and login.

## First Run

On the first agentic task that uses this server, `uvx` will download the package from GitHub before invoking it. Subsequent runs use the cached version. Ensure the CraterClaw machine has internet access for the initial download.

## Available Tools

Once connected, the MCP server exposes these tools to the model:

- `list_torrents` — list torrents, optionally filtered by status, category, or tag
- `add_torrent_url` — add a torrent via URL or magnet link
- `pause_torrent` / `resume_torrent` — pause or resume a specific torrent
- `get_qb_transfer_info` — current upload and download speeds
- `get_qb_app_preferences` — qBitTorrent application settings
