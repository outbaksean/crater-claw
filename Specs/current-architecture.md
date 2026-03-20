# Current Architecture

## Solution Structure
- `CraterClaw.Core` — core library (C#, .NET 10)
- `CraterClaw.Console` — console harness (C#, .NET 10)
- `CraterClaw.Core.Tests` — xUnit test project (.NET 10)

## Configuration
- `craterclaw.json` in the console output directory — provider endpoints, MCP server definitions, qBitTorrent connection details.
- User secrets (via .NET user secrets) — credentials and secret values.
- `${VAR_NAME}` references in config values are resolved from OS user-level environment variables at point of use.

## CraterClaw.Core

### Provider Services
- `IProviderStatusService` — checks reachability of a provider endpoint.
- `IModelListingService` — lists downloaded models at a provider endpoint.
- `IModelExecutionService` — sends a conversational prompt to a model and returns the response (Semantic Kernel-backed via Ollama chat completion).
- `IAgenticExecutionService` (`SemanticKernelAgenticExecutionService`) — runs a Semantic Kernel tool-use loop. Sends a prompt, processes function calls, invokes tools via SK kernel, and iterates until the model stops calling tools or the iteration limit is reached. Supports optional streaming output via `AgenticRequest.StreamChunk`.

### Configuration Types
- `ProviderOptions` — named collection of endpoints (`BaseUrl`); `Active` names the default.
- `McpOptions` — named collection of MCP server definitions (transport, URL or command, enabled flag).
- `QBitTorrentOptions` — `BaseUrl`, `Username`, `Password`; bound to the `qbittorrent` config section.

### MCP
- `IMcpAvailabilityService` — checks whether a configured MCP server is reachable.
- `IMcpClientProvider` / `McpClientProvider` — registered for future MCP tool integration.

### Behavior Profiles
- `IBehaviorProfileService` — returns the fixed profile catalog.
- Fixed profiles: `no-tools` (no tools permitted), `qbittorrent-manager` (qBitTorrent plugin permitted).
- Each profile has an id, name, description, and a set of permitted MCP server names.

### Plugins
- `QBitTorrentPlugin` — Semantic Kernel kernel plugin. Authenticates with the qBitTorrent WebUI using cookie-based login (`/api/v2/auth/login`), caches the SID cookie, and re-authenticates on 403 responses. Kernel functions:
  - `ListTorrents` — JSON array of all torrents (name, state, added_on).
  - `AddTorrentByUrl` — adds a torrent from a magnet link or HTTP URL.
  - `PauseTorrent` — pauses a torrent by hash.
  - `ResumeTorrent` — resumes a torrent by hash.
  - `DeleteTorrent` — deletes a torrent by hash with optional file deletion.
  - `GetTransferStats` — current download/upload speeds and session totals.

### Logging
- Serilog with a rolling daily file sink in `logs/` relative to the console output directory.
- Minimum level: Debug.
- Registered via `AddLogging(b => b.AddSerilog(...))`.

## Console Harness Flow
1. Load `craterclaw.json` and user secrets.
2. Display numbered list of configured endpoints; prompt for selection (blank = use default).
3. Check endpoint reachability; display result.
4. If reachable: list downloaded models; prompt for model selection.
5. If model selected: prompt for an interactive message; display the response.
6. Display numbered list of configured MCP servers; prompt to check availability of one.
7. Display numbered list of behavior profiles; prompt for selection.
8. If profile selected and has allowed tools: list available plugin functions by name and description.
9. If model selected: prompt for a task prompt; run agentic execution with streaming output; display tools invoked and finish reason.
