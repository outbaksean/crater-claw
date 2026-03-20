# CraterClaw Library qBitTorrent Plugin Spec

## Name
- CraterClaw Library qBitTorrent Plugin

## Purpose
- Provide a Semantic Kernel kernel plugin exposing qBitTorrent WebUI operations as kernel functions, wire it into the agentic execution loop, and update the console harness to run end-to-end agentic tasks with streaming output.

## Scope
- Add `QBitTorrentOptions` bound to a `qbittorrent` section in `craterclaw.json` with fields: `BaseUrl`, `Username`, `Password`.
- Implement `QBitTorrentPlugin`, an SK kernel plugin with `[KernelFunction]` methods:
  - `ListTorrents` — returns a JSON array of all torrents with name, state, and added_on.
  - `AddTorrentByUrl` — adds a torrent from a magnet link or HTTP URL.
  - `PauseTorrent` — pauses a torrent by hash.
  - `ResumeTorrent` — resumes a torrent by hash.
  - `DeleteTorrent` — deletes a torrent by hash with optional file deletion.
  - `GetTransferStats` — returns current download/upload speeds and session totals.
- Authentication: cookie-based login via `/api/v2/auth/login`. The plugin caches the SID cookie and re-authenticates on 403 responses.
- Add `StreamChunk: Action<string>?` to `AgenticRequest` so callers can receive streaming output chunks.
- Update `SemanticKernelAgenticExecutionService` to support streaming: when `StreamChunk` is set, use `GetStreamingChatMessageContentsAsync` and collect function calls via `FunctionCallContentBuilder`; otherwise use the existing non-streaming path.
- Register `QBitTorrentOptions` and `QBitTorrentPlugin` in `ServiceCollectionExtensions.AddCraterClawCore()`.
- Update the console harness:
  - After profile selection, if the profile has allowed tools, list the available plugin functions by name and description.
  - If a model is also selected, prompt for a task prompt and run an agentic execution with `StreamChunk` writing chunks to `Console.Write`. Display tools invoked and finish reason after completion.
  - Retain the MCP server listing and availability check flow (used for non-plugin MCP servers in future).

## Contract Notes
- `QBitTorrentPlugin` is a public class in `CraterClaw.Core` with `[KernelPlugin]` decoration.
- All kernel functions return `string` (JSON or plain text).
- `QBitTorrentOptions` validation: `BaseUrl` must be a valid URI; `Username` and `Password` are required.
- Credentials are stored in user secrets under `qbittorrent:baseUrl`, `qbittorrent:username`, `qbittorrent:password`.
- Tests use a mock `HttpMessageHandler` and do not require a live qBitTorrent instance.

## Manual Verification
- Prerequisites: qBitTorrent running with WebUI enabled; credentials set in user secrets.
- Run the console harness, select an endpoint, select a model, then select the `qbittorrent-manager` profile.
- Confirm the six plugin functions are listed by name and description.
- Enter a task prompt; confirm streaming output appears and tool invocation summary is printed.
- Select the `no-tools` profile; confirm no plugin functions are listed and agentic execution runs without tools.

## Status
- Done
