# Current Architecture

## Solution Structure
- `CraterClaw.Core` — core library (C#, .NET 10)
- `CraterClaw.Console` — console harness (C#, .NET 10)
- `CraterClaw.Api` — ASP.NET Core minimal API (C#, .NET 10)
- `CraterClaw.Core.Tests` — xUnit unit tests (.NET 10)
- `CraterClaw.Api.Tests` — xUnit integration tests using `WebApplicationFactory` (.NET 10)

## Developer Tooling

- `tools/CraterClaw.psm1` — PowerShell module exporting the `craterclaw` command. Reads `CRATERCLAW_ROOT` env var at runtime. Subcommands: `run`, `build`, `test`, `format`. Opens separate windows for API and web dev server; runs console harness in the current terminal.
- `tools/Install-CraterClaw.ps1` — idempotent install script. Copies the module to the user's PowerShell modules directory, sets `CRATERCLAW_ROOT` as a persistent user environment variable, and adds `Import-Module CraterClaw` to the profile. Supports both PowerShell 7 (Core) and Windows PowerShell 5.1.

## Formatting
- `.editorconfig` — LF line endings everywhere, 4-space indent for C#/JSON, 2-space for JS/TS/Vue/MJS.
- `.vscode/settings.json` — format on save enabled; Prettier is the default formatter for web files, C# extension for `.cs` files.
- C#: `dotnet format` reads style rules from `.editorconfig`.
- Vue/TS: Prettier via `npm run lint:fix`; configured in `.prettierrc.json` with `endOfLine: lf`.

## Configuration
- `craterclaw.json` in the console output directory — provider endpoints, MCP server definitions, qBitTorrent connection details.
- User secrets (via .NET user secrets) — credentials and secret values.
- `${VAR_NAME}` references in config values are resolved from OS user-level environment variables at point of use.

### Configuration Types
- `ProviderOptions` — named collection of endpoints (`BaseUrl`); `Active` names the default.
- `McpOptions` — named collection of MCP server definitions (transport, URL or command, enabled flag).
- `QBitTorrentOptions` — `BaseUrl`, `Username`, `Password`; bound to the `qbittorrent` config section.
- `AiLoggingOptions` — `Enabled` (bool, default false), `Path` (string). Accepts a directory path or a file prefix. Bound to the `aiLogging` config section. No validator.

## CraterClaw.Core

### Provider Services
- `IProviderStatusService` — checks reachability of a provider endpoint.
- `IModelListingService` — lists downloaded models at a provider endpoint.
- `IModelExecutionService` — sends a conversational prompt to a model and returns the response (Semantic Kernel-backed via Ollama chat completion).
- `IAgenticExecutionService` (`SemanticKernelAgenticExecutionService`) — runs a Semantic Kernel tool-use loop. Sends a prompt, processes function calls, invokes tools via SK kernel, and iterates until the model stops calling tools or the iteration limit is reached. Supports optional streaming output via `AgenticRequest.StreamChunk`.

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
  - `SearchTorrents` — starts a search job using installed qBitTorrent search plugins, polls until complete, returns a JSON array of results (fileName, fileUrl, fileSize, nbSeeders, nbLeechers, siteUrl). `maxResults` defaults to 10. File names are truncated to 120 characters and magnet link tracker parameters are stripped to reduce response size.

### Logging
- Both the console and API use Serilog with sub-logger routing.
- Main log: rolling daily file in `logs/` relative to the application base directory. Contains lifecycle events, warnings, and errors. The `CraterClaw.AiTraffic` category and `System.Net.Http` namespace are excluded.
- AI log: rolling daily `.log` file written only when `aiLogging.enabled` is `true`. `aiLogging.path` may be a directory (files written as `ai-{date}.log` inside it) or a file prefix; defaults to `logs/ai-{date}.log`. Contains only `CraterClaw.AiTraffic` events: full Ollama request JSON and full response content with no truncation.
- `OllamaModelExecutionService` and `SemanticKernelAgenticExecutionService` each hold a named logger `_aiLogger = loggerFactory.CreateLogger("CraterClaw.AiTraffic")` for AI-traffic detail.
- Sensitive values (search queries, qBitTorrent credentials/URL) are not logged.
- Minimum level: Debug. Both console and API apply `MinimumLevel.Override("System.Net.Http", Warning)` to suppress HTTP client request logs. The API additionally overrides `Microsoft` and `System` namespaces.
- Registered via `AddLogging(b => b.AddSerilog(...))` in the console; via `builder.Host.UseSerilog(...)` in the API.

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
- `useProfiles` composable: fetches profile list, tracks selected profile.
- `ProfileSelector` component: numbered list of profiles with name and description.
- `AgenticPanel` component: task prompt input, displays response content, finish reason, and tools invoked list.
- `App.vue`: provider list, status indicator, model list, chat panel, profile selector, agentic panel (shown when provider + model + profile are all selected).

ESLint is configured via `eslint.config.mjs` using flat config format with `eslint-plugin-vue` (flat/essential), `@vue/eslint-config-typescript`, and `@vue/eslint-config-prettier`. Vitest globals (`describe`, `it`, `test`, `expect`, `vi`, etc.) are registered for `*.spec.ts` and `*.test.ts` files. Prettier is configured with `endOfLine: lf` for cross-platform consistency. `npm run lint` and `npm run lint:fix` are available.

MCP server UI is not implemented in the frontend. The API endpoints exist but are not surfaced in the Vue app.

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
