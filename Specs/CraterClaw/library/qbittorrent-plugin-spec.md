# CraterClaw Library qBitTorrent Plugin Spec

## Name
- CraterClaw Library qBitTorrent Plugin

## Purpose
- Provide a Semantic Kernel kernel plugin that exposes qBitTorrent WebUI operations as kernel functions, so the agentic execution loop can manage torrents without a subprocess MCP server.

## Scope
- Add a `QBitTorrentOptions` configuration type bound to a `qbittorrent` section in `craterclaw.json`, with fields: `BaseUrl`, `Username`, `Password`.
- Implement `QBitTorrentPlugin`, an SK kernel plugin class with `[KernelFunction]` methods covering core torrent management operations:
  - `ListTorrents` - returns a summary list of all torrents (name, hash, status, progress, size)
  - `AddTorrentByUrl` - adds a torrent from a magnet link or HTTP URL
  - `PauseTorrent` - pauses a torrent by hash
  - `ResumeTorrent` - resumes a torrent by hash
  - `DeleteTorrent` - deletes a torrent by hash with optional file deletion
  - `GetTransferStats` - returns current download/upload speed and session totals
- Authentication uses the qBitTorrent WebUI cookie-based login (`/api/v2/auth/login`). The plugin logs in once and caches the session cookie, re-authenticating on 403 responses.
- Register `QBitTorrentOptions` and `QBitTorrentPlugin` in `ServiceCollectionExtensions.AddCraterClawCore()`.
- Update the console harness: when the `qbittorrent-manager` profile is selected, list the available plugin functions by name and description in place of the MCP tool listing.
- Remove the MCP tool listing from Program.cs (the `IMcpClientProvider` loop), as the plugin replaces it for the qBitTorrent use case. `IMcpClientProvider` and `McpClientProvider` remain registered for future MCP servers.

## Contract Notes
- `QBitTorrentPlugin` is a public class in `CraterClaw.Core` decorated with `[KernelPlugin]`.
- All kernel functions return `string` (JSON or plain text) so SK can include them in the tool-use loop without custom serialization.
- `QBitTorrentOptions` validation: `BaseUrl` is required and must be a valid URI; `Username` and `Password` are required.
- Credentials are stored in user secrets under `qbittorrent:baseUrl`, `qbittorrent:username`, `qbittorrent:password`.
- Tests use a mock `HttpMessageHandler` and do not require a live qBitTorrent instance.

## Manual Verification
- Prerequisites: qBitTorrent running with WebUI enabled; credentials set in user secrets.
- Run the console harness and select the `qbittorrent-manager` profile.
- Confirm the six plugin functions are listed by name and description.
- Confirm that selecting the `no-tools` profile shows no functions listed.

## Status
- Done
