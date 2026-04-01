# Current Architecture

## Solution Structure

- `CraterClaw.Core` — core library (C#, .NET 10)
- `CraterClaw.Console` — console harness (C#, .NET 10)
- `CraterClaw.Api` — ASP.NET Core minimal API (C#, .NET 10)
- `CraterClaw.Core.Tests` — xUnit unit tests (.NET 10)
- `CraterClaw.Api.Tests` — xUnit integration tests using `WebApplicationFactory` (.NET 10)

## Developer Tooling

- `tools/CraterClaw.psm1` — PowerShell module exporting the `craterclaw` command. Reads `CRATERCLAW_ROOT` env var at runtime. Subcommands: `run`, `build`, `test`, `format`. Opens separate windows for API and web dev server; runs console harness in the current terminal. `run` accepts `-Config <path>` to pass an alternate config file to the .NET app; path is resolved to absolute before forwarding.
- `tools/Install-CraterClaw.ps1` — idempotent install script. Copies the module to the user's PowerShell modules directory, sets `CRATERCLAW_ROOT` as a persistent user environment variable, and adds `Import-Module CraterClaw` to the profile. Supports both PowerShell 7 (Core) and Windows PowerShell 5.1.

## Formatting

- `.editorconfig` — LF line endings everywhere, 4-space indent for C#/JSON, 2-space for JS/TS/Vue/MJS.
- `.vscode/settings.json` — format on save enabled; Prettier is the default formatter for web files, C# extension for `.cs` files.
- C#: `dotnet format` reads style rules from `.editorconfig`.
- Vue/TS: Prettier via `npm run lint:fix`; configured in `.prettierrc.json` with `endOfLine: lf`.

## Configuration

- `craterclaw.json` in the console output directory — provider endpoints, behavior definitions with plugin bindings.
- User secrets (via .NET user secrets) — credentials and secret values.
- `${VAR_NAME}` references in config values are resolved from OS user-level environment variables at point of use.
- Config path resolution priority (highest to lowest): `CRATERCLAW_CONFIG` environment variable, `--config <path>` CLI argument, default `AppContext.BaseDirectory/craterclaw.json`. The PowerShell `-Config` parameter forwards the resolved absolute path as `--config`.

### Configuration Types

- `ProviderOptions` — named collection of endpoints (`BaseUrl`); `Active` names the default.
- `AiLoggingOptions` — `Enabled` (bool, default false), `Path` (string). Accepts a directory path or a file prefix. Bound to the `aiLogging` config section. No validator.
- `AiRawLoggingOptions` — `Enabled` (bool, default false), `Path` (string). Same resolution rules as `AiLoggingOptions`. Bound to the `aiRawLogging` config section. No validator.
- `BehaviorEntry` / `PluginEntry` — POCO types bound to `behaviors` section of config. `BehaviorEntry` has `Name`, `Description`, `SystemPrompt`, `PreferredProviderName`, `PreferredModelName`, and a `List<PluginEntry>`. `PluginEntry` has `Name`, `Tools`, and `Config` (dictionary of string key/value for per-binding plugin connection settings).

## CraterClaw.Core

### Provider Services

- `IProviderStatusService` — checks reachability of a provider endpoint.
- `IModelListingService` — lists downloaded models at a provider endpoint.
- `IModelExecutionService` — sends a conversational prompt to a model and returns the response (Semantic Kernel-backed via Ollama chat completion).
- `IAgenticExecutionService` (`SemanticKernelAgenticExecutionService`) — runs a Semantic Kernel tool-use loop. Sends a prompt, processes function calls, invokes tools via SK kernel, and iterates until the model stops calling tools or the iteration limit is reached. Supports optional streaming output via `AgenticRequest.StreamChunk` and optional thinking token streaming via `AgenticRequest.StreamThinkingChunk`. Uses `OllamaPromptExecutionSettings`. Thinking tokens are detected from `chunk.InnerContent as OllamaSharp.Models.Chat.ChatResponseStream` — `stream.Message?.Thinking` is non-null during thinking phases.

### Behavior Profiles

- `IBehaviorProfileService` / `BehaviorProfileService` — reads behavior definitions from `IOptions<Dictionary<string, BehaviorEntry>>` (bound to the `behaviors` config section). Maps each entry to a `BehaviorProfile` record.
- `BehaviorProfile` — `Id`, `Name`, `Description`, `SystemPrompt`, `PreferredProviderName` (nullable), `PreferredModelName` (nullable), `Plugins` (list of `PluginBinding`).
- `PluginBinding` — `Name`, `Tools` (allowlist; empty means all), `Config` (per-binding connection settings dictionary).
- Default profiles in `craterclaw.json`: `no-tools` (no plugins), `qbittorrent-home` (localhost qBitTorrent), `qbittorrent-seedbox` (remote qBitTorrent).

### Plugin Registry

- `IPluginRegistry` / `DefaultPluginRegistry` — resolves a list of `PluginBinding` values into `IReadOnlyList<KernelPlugin>`. Holds a dictionary of named factory delegates `Func<IReadOnlyDictionary<string, string>, object>`. For each binding: invokes the factory with `binding.Config`, creates a `KernelPlugin` via `KernelPluginFactory.CreateFromObject`, then filters to the `Tools` allowlist using `KernelPluginFactory.CreateFromFunctions` if the list is non-empty. Unknown plugin names are logged and skipped. Unknown tool names are logged and skipped.
- Registered factories: `"qbittorrent"` — creates a `QBitTorrentPlugin` from config keys `baseUrl`, `username`, `password`.
- Named `HttpClient` registrations: `"qbittorrent"` (plain), `"ollama"` (10-minute timeout, `OllamaLoggingHandler` attached). `DefaultKernelFactory` uses the `"ollama"` client. `OllamaSharp` is a direct package reference in `CraterClaw.Core` (required to access `ChatResponseStream` for thinking token detection; the SK connector pulls it transitively but a direct reference is needed to use its types).

### Plugins

- `QBitTorrentPlugin` — Semantic Kernel kernel plugin. Takes `QBitTorrentOptions` directly (not IOptions). Authenticates with the qBitTorrent WebUI using cookie-based login (`/api/v2/auth/login`), caches the SID cookie, and re-authenticates on 403 responses. Kernel functions:
    - `ListTorrents` — JSON array of all torrents (name, state, added_on).
    - `AddTorrentByUrl` — adds a torrent from a magnet link or HTTP URL.
    - `PauseTorrent` — pauses a torrent by hash.
    - `ResumeTorrent` — resumes a torrent by hash.
    - `DeleteTorrent` — deletes a torrent by hash with optional file deletion.
    - `GetTransferStats` — current download/upload speeds and session totals.
    - `SearchTorrents` — starts a search job using installed qBitTorrent search plugins, polls until complete, returns a JSON array of results (fileName, fileUrl, fileSize, nbSeeders, nbLeechers, siteUrl). `maxResults` defaults to 10. File names are truncated to 120 characters and magnet link tracker parameters are stripped to reduce response size.

### Logging

- Both the console and API use Serilog with sub-logger routing.
- Main log: rolling daily file in `logs/` relative to the application base directory. Contains lifecycle events, warnings, and errors. All `CraterClaw.AiTraffic` sub-categories and `System.Net.Http` namespace are excluded (filter uses `StartsWith("CraterClaw.AiTraffic")`).
- AI log: rolling daily `.log` file written only when `aiLogging.enabled` is `true`. `aiLogging.path` may be a directory (files written as `ai-{date}.log` inside it) or a file prefix; defaults to `logs/ai-{date}.log`. Contains only `CraterClaw.AiTraffic` events: full Ollama request JSON and full response content with no truncation.
- Raw HTTP log: rolling daily `.log` file written only when `aiRawLogging.enabled` is `true`. `aiRawLogging.path` follows the same resolution rules as `aiLogging.path`; defaults to `logs/ollama-raw-{date}.log` (console) / `logs/ollama-api-raw-{date}.log` (API). Contains only `CraterClaw.AiTraffic.Raw` events: raw Ollama HTTP request bodies (`[REQUEST]`) and response bodies (`[RESPONSE]`). Response bodies are captured via `TeeHttpContent` / `TeeStream` which pass data through without buffering the full response before delivery.
- `OllamaModelExecutionService` and `SemanticKernelAgenticExecutionService` each hold a named logger `_aiLogger = loggerFactory.CreateLogger("CraterClaw.AiTraffic")` for AI-traffic detail.
- `OllamaLoggingHandler` — `DelegatingHandler` registered on the named `"ollama"` `HttpClient`. Reads and logs the request body, then wraps `response.Content` with `TeeHttpContent`.
- `TeeHttpContent` — wraps an `HttpContent`, tees bytes to a `MemoryStream` accumulator as they are read (via `TeeStream` for the `ReadAsStreamAsync` path; via direct copy for the `SerializeToStreamAsync` path). Logs the accumulated response body to `CraterClaw.AiTraffic.Raw` on `Dispose`.
- `TeeStream` — pass-through `Stream` that writes a copy of every read to a shared `MemoryStream` accumulator owned by `TeeHttpContent`.
- Sensitive values (search queries, qBitTorrent credentials/URL) are not logged.
- Minimum level: Debug. Both console and API apply `MinimumLevel.Override("System.Net.Http", Warning)` to suppress HTTP client request logs. The API additionally overrides `Microsoft` and `System` namespaces.
- Registered via `AddLogging(b => b.AddSerilog(...))` in the console; via `builder.Host.UseSerilog(...)` in the API.

## CraterClaw.Api

ASP.NET Core minimal API. Loads `craterclaw.json` (optional, falls back to in-memory/environment config) and user secrets. Registers `AddCraterClawCore`. CORS is permissive (all origins, headers, methods) for development.

### File Layout

- `Program.cs` — startup and DI wiring only. Calls `app.MapProviderEndpoints()` and `app.MapProfileEndpoints()`.
- `Models/ApiModels.cs` — all request/response record types used by the API.
- `Endpoints/ProvidersEndpoints.cs` — `ProvidersEndpoints.MapProviderEndpoints(WebApplication)` extension method registering all provider-related routes.
- `Endpoints/ProfilesEndpoints.cs` — `ProfilesEndpoints.MapProfileEndpoints(WebApplication)` extension method registering the profiles route.
- `Services/IProviderResolver.cs` — `IProviderResolver` interface: `Resolve(string name) -> ProviderEndpoint?`, `GetAll() -> IEnumerable<ProviderEndpoint>`.
- `Services/ProviderResolver.cs` — singleton implementation backed by `IOptions<ProviderOptions>`. Endpoint handlers inject `IProviderResolver` instead of `IOptions<ProviderOptions>` directly.

### Endpoints

- `GET /api/providers` — returns all configured endpoint names and base URLs from `IProviderResolver.GetAll()`.
- `GET /api/providers/{name}/status` — calls `IProviderStatusService.CheckStatusAsync`, returns `{ isReachable, errorMessage }`. 404 if name not found.
- `GET /api/providers/{name}/models` — calls `IModelListingService.ListModelsAsync`, returns `[{ name, sizeBytes, modifiedAt }]`. 404 if name not found.
- `POST /api/providers/{name}/execute` — accepts `{ modelName, messages: [{role, content}], temperature?, maxTokens? }`, calls `IModelExecutionService.ExecuteAsync`, returns `{ content, modelName, finishReason }`. 404 if name not found.
- `GET /api/profiles` — returns all behavior profiles from `IBehaviorProfileService`. Response shape: `{ id, name, description, systemPrompt, preferredProviderName, preferredModelName, plugins: [{ name, tools }] }`. Plugin `config` is excluded from the response (credentials not exposed).
- `POST /api/providers/{name}/agentic` — accepts `{ modelName, prompt, profileId, maxIterations? }`, resolves profile via `IBehaviorProfileService`, builds plugin list (same logic as console), calls `IAgenticExecutionService.ExecuteAsync` with `StreamChunk: null`, returns `{ content, finishReason, toolsInvoked }`. 404 if endpoint not found, 400 if profile not found.
- `POST /api/providers/{name}/agentic/stream` — accepts `{ modelName, prompt, profileId, maxIterations?, showThinking? }`. Returns `text/event-stream`. When `showThinking` is true, emits `{"type":"thinking","content":"..."}` events for thinking tokens before or alongside content. Streams `{"type":"chunk","content":"..."}` events as text arrives, followed by a `{"type":"done","finishReason":"...","toolsInvoked":[...]}` event when complete. SSE JSON is camelCase with string enum values. 404 / 400 on unknown provider / profile (set before headers are sent).

`AgenticRequest.StreamChunk` and `AgenticRequest.StreamThinkingChunk` are both `Func<string, Task>?` (async). Enums are serialized as strings (`JsonStringEnumConverter` applied globally). Internal types are visible to `CraterClaw.Api.Tests` via `InternalsVisibleTo`.

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
- `useAgentic` composable: wraps `streamAgentic`; exposes `content`, `thinking` (builds up from thinking events), `showThinking` (boolean ref, default false; when true, `run` adds `showThinking: true` to the request), `finishReason`, `toolsInvoked`, `loading`, `error`, `run(providerName, request)`, and `cancel()`. Used by `AgenticPanel`.
- `useBehaviorDefaults` composable: takes `providers` and `models` refs and `selectProvider`/`selectModel` callbacks. `applyProfileDefaults(profile)` applies preferred provider/model defaults from the profile, calling the appropriate select function if the preferred value is found, or pushing a warning string to `behaviorWarnings` if not. Warnings are cleared on each call.
- `ProfileSelector` component: numbered list of profiles with name and description.
- `AgenticPanel` component: task prompt input with a "show thinking" checkbox. Displays thinking tokens in a collapsed `<details>` block (`.thinking-content`) when present. Displays response content, finish reason, and tools invoked list.
- `App.vue`: provider list, status indicator, model list, chat panel, profile selector (with inline behavior warnings), agentic panel (shown when provider + model + profile are all selected). When a profile is selected, `applyProfileDefaults` is called to apply preferred provider/model and surface warnings.

### API Types (`src/api/types.ts`)

- `BehaviorProfile` — `id`, `name`, `description`, `systemPrompt`, `preferredProviderName` (null or string), `preferredModelName` (null or string), `plugins` (array of `PluginBinding`).
- `PluginBinding` — `name`, `tools` (string array).

ESLint is configured via `eslint.config.mjs` using flat config format with `eslint-plugin-vue` (flat/essential), `@vue/eslint-config-typescript`, and `@vue/eslint-config-prettier`. Vitest globals (`describe`, `it`, `test`, `expect`, `vi`, etc.) are registered for `*.spec.ts` and `*.test.ts` files. Prettier is configured with `endOfLine: lf` for cross-platform consistency. `npm run lint` and `npm run lint:fix` are available.

## Console Harness Flow

1. Load config file (path resolved from `CRATERCLAW_CONFIG` env var, `--config` arg, or default `craterclaw.json`) and user secrets.
2. Display numbered list of configured endpoints; prompt for selection (blank = use default).
3. Check endpoint reachability; display result.
4. If reachable: list downloaded models; prompt for model selection.
5. If model selected: prompt for an interactive message; display the response.
6. Display numbered list of behavior profiles; prompt for selection.
7. If profile selected: apply preferred provider and model defaults (switch endpoint/re-fetch models if provider changed; switch selected model if model found; print warning if not found).
8. If profile selected and has plugins: list available kernel functions by name and description.
9. If model selected: prompt for a task prompt; run agentic execution with streaming output; display tools invoked and finish reason.
