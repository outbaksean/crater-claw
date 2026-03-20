# Current Architecture

## Solution Structure
- `CraterClaw.Core` — core library (C#, .NET 10)
- `CraterClaw.Console` — console harness (C#, .NET 10)
- `CraterClaw.Api` — ASP.NET Core minimal API (C#, .NET 10)
- `CraterClaw.Core.Tests` — xUnit unit tests (.NET 10)
- `CraterClaw.Api.Tests` — xUnit integration tests using `WebApplicationFactory` (.NET 10)

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

## CraterClaw.Api

ASP.NET Core minimal API. Loads `craterclaw.json` (optional, falls back to in-memory/environment config) and user secrets. Registers `AddCraterClawCore`. CORS is permissive (all origins, headers, methods) for development.

### Endpoints
- `GET /api/providers` — returns all configured endpoint names and base URLs from `ProviderOptions`.
- `GET /api/providers/{name}/status` — calls `IProviderStatusService.CheckStatusAsync`, returns `{ isReachable, errorMessage }`. 404 if name not found.
- `GET /api/providers/{name}/models` — calls `IModelListingService.ListModelsAsync`, returns `[{ name, sizeBytes, modifiedAt }]`. 404 if name not found.
- `POST /api/providers/{name}/execute` — accepts `{ modelName, messages: [{role, content}], temperature?, maxTokens? }`, calls `IModelExecutionService.ExecuteAsync`, returns `{ content, modelName, finishReason }`. 404 if name not found.
- `GET /api/profiles` — returns all behavior profiles from `IBehaviorProfileService`.
- `GET /api/mcp` — returns configured MCP server names, labels, and enabled flags from `McpOptions`.
- `POST /api/mcp/{name}/availability` — calls `IMcpAvailabilityService.CheckAvailabilityAsync`, returns `{ name, isAvailable, errorMessage }`. 404 if name not found.
- `POST /api/providers/{name}/agentic` — accepts `{ modelName, prompt, profileId, maxIterations? }`, resolves profile via `IBehaviorProfileService`, builds plugin list (same logic as console), calls `IAgenticExecutionService.ExecuteAsync` with `StreamChunk: null`, returns `{ content, finishReason, toolsInvoked }`. 404 if endpoint not found, 400 if profile not found.

Enums are serialized as strings (`JsonStringEnumConverter` applied globally).

## CraterClaw.Web

Vue 3 TypeScript frontend (Vite, Vitest). Consumes `CraterClaw.Api` over HTTP. API base URL read from `VITE_API_BASE_URL` environment variable (defaults to `http://localhost:5000`).

### Project Layout
- `src/api/types.ts` — shared TypeScript types mirroring all API response shapes.
- `src/api/client.ts` — typed `fetch` wrappers for all API endpoints.
- `src/composables/` — Vue composables for stateful data fetching.
- `src/components/` — reusable UI components (populated in later phases).
- `src/App.vue` — root component; wires composables to UI.

### Implemented
- `getProviders`, `getProviderStatus`, `getModels` in `client.ts`.
- `useProviders` composable: fetches provider list, tracks selected provider, fetches and exposes status.
- `useModels` composable: fetches models for selected provider, tracks selected model.
- `useExecution` composable: manages conversation message history, calls `postExecute`, appends user and assistant turns.
- `InteractiveChat` component: input form, conversation history display, loading/error state.
- `App.vue`: provider list, status indicator, model list, chat panel (shown when model is selected).

### Planned (later phases)
- Behavior profile selection and agentic execution panel
- MCP server list and availability check panel

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
