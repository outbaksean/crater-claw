# qBitTorrent MCP Server Setup

This document covers installing and running the qBitTorrent MCP server for use with CraterClaw. The server runs on the same machine as qBitTorrent (or any machine with network access to it) and exposes an SSE HTTP endpoint that CraterClaw connects to remotely.

The MCP server is part of the [yarr-mcp](https://github.com/jmagar/yarr-mcp) project and is not part of this repository.

## Prerequisites

- Python 3.10 or later
- `uv` package manager (`pip install uv` or see https://docs.astral.sh/uv)
- A running qBitTorrent instance with the WebUI enabled
- Network access from the CraterClaw host to the machine running this server

## Installation

Run these commands on the machine where qBitTorrent is hosted:

```bash
git clone https://github.com/jmagar/yarr-mcp.git
cd yarr-mcp
uv sync
```

## Configuration

Copy the example environment file and fill in your values:

```bash
cp .env.example .env
```

Edit `.env` with the following values:

```
QBITTORRENT_URL=http://localhost:8080
QBITTORRENT_USER=admin
QBITTORRENT_PASS=your-password
QBITTORRENT_MCP_TRANSPORT=sse
QBITTORRENT_MCP_HOST=0.0.0.0
QBITTORRENT_MCP_PORT=8000
```

- `QBITTORRENT_URL` — the qBitTorrent WebUI URL as seen from this machine
- `QBITTORRENT_MCP_HOST=0.0.0.0` — binds to all interfaces so CraterClaw can reach it over the network
- `QBITTORRENT_MCP_PORT` — port to expose; open this port in the firewall if needed

## Running the Server

```bash
source .venv/bin/activate
python src/qbittorrent-mcp/qbittorrent-mcp-server.py
```

The server listens for MCP connections at `http://<host>:<port>/mcp`.

## CraterClaw Configuration

Add an entry to your `mcp-config.json` (see mcp-config spec for the full schema):

```json
{
  "servers": [
    {
      "name": "qbittorrent",
      "label": "qBitTorrent",
      "transport": "http",
      "baseUrl": "http://192.168.1.x:8000",
      "enabled": true
    }
  ]
}
```

Replace `192.168.1.x` with the IP or hostname of the machine running the MCP server.

## Verifying the Server is Reachable

From the CraterClaw host, confirm the server is up:

```bash
curl http://192.168.1.x:8000/mcp
```

A response of any kind (including an error about missing headers) confirms the server is reachable. A connection refused error means the server is not running or the port is blocked.

## Available Tools

Once connected, the server exposes these tools to the model:

- `list_torrents` — list torrents, optionally filtered by status, category, or tag
- `add_torrent_url` — add a torrent via URL or magnet link
- `pause_torrent` / `resume_torrent` — pause or resume a specific torrent
- `get_qb_transfer_info` — current upload and download speeds
- `get_qb_app_preferences` — qBitTorrent application settings
