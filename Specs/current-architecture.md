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
- `BehaviorEntry` / `PluginEntry` — POCO types bound to `behaviors` section of config. `BehaviorEntry` has `Name`, `Description`, `SystemPrompt`, `PreferredProviderName`, `PreferredModelName`, `MaxContext` (nullable int), and a `List<PluginEntry>`. `PluginEntry` has `Name`, `Tools`, and `Config` (dictionary of string key/value for per-binding plugin connection settings).

## CraterClaw.Core

### Provider Services

- `IProviderStatusService` — checks reachability of a provider endpoint.
- `IModelListingService` — lists downloaded models at a provider endpoint.
- `IModelExecutionService` — sends a conversational prompt to a model and returns the response (Semantic Kernel-backed via Ollama chat completion).
- `IAgenticExecutionService` (`SemanticKernelAgenticExecutionService`) — runs a Semantic Kernel tool-use loop. Sends a prompt, processes function calls, invokes tools via SK kernel, and iterates until the model stops calling tools or the iteration limit is reached. Supports optional streaming output via `AgenticRequest.StreamChunk` and optional thinking token streaming via `AgenticRequest.StreamThinkingChunk`. Uses `OllamaPromptExecutionSettings`. Thinking tokens are detected from `chunk.InnerContent as OllamaSharp.Models.Chat.ChatResponseStream` — `stream.Message?.Thinking` is non-null during thinking phases.

### Behavior Profiles

- `IBehaviorProfileService` / `BehaviorProfileService` — reads behavior definitions from `IOptions<Dictionary<string, BehaviorEntry>>` (bound to the `behaviors` config section). Maps each entry to a `BehaviorProfile` record.
- `BehaviorProfile` — `Id`, `Name`, `Description`, `SystemPrompt`, `PreferredProviderName` (nullable), `PreferredModelName` (nullable), `MaxContext` (nullable int), `Hidden` (bool, default false), `Plugins` (list of `PluginBinding`). `GetAll()` excludes hidden profiles; `GetById()` returns any profile regardless of `Hidden`.
- `PluginBinding` — `Name`, `Tools` (allowlist; empty means all), `Config` (per-binding connection settings dictionary).
- Default profiles in `craterclaw.json`: `no-tools` (no plugins), `qbittorrent-seedbox` (remote qBitTorrent), `story-director` (parent, two subagent plugins, `qwen3:14b`), `story-planner` (hidden child, `qwen3:8b`), `story-writer` (hidden child, `gemma3:12b`).

### Plugin Registry

- `IPluginRegistry` / `DefaultPluginRegistry` — resolves a list of `PluginBinding` values into `IReadOnlyList<KernelPlugin>`. Holds two factory dictionaries: `Func<IReadOnlyDictionary<string, string>, object>` (object factories, processed via `KernelPluginFactory.CreateFromObject`) and `Func<IReadOnlyDictionary<string, string>, KernelPlugin>` (plugin factories, returning a `KernelPlugin` directly). Plugin factories are checked first. For object factories: creates a `KernelPlugin` via `KernelPluginFactory.CreateFromObject`, then filters to the `Tools` allowlist using `KernelPluginFactory.CreateFromFunctions` if the list is non-empty. Unknown plugin names are logged and skipped. Unknown tool names are logged and skipped.
- Registered object factories: `"qbittorrent"` — creates a `QBitTorrentPlugin` from config keys `baseUrl`, `username`, `password`.
- Registered plugin factories: `"subagent"` — creates a `SubAgentPlugin` and wraps it in a `KernelPlugin` with a dynamically named function. Config keys: `profileId` (required), `functionName` (required), `description` (optional). The plugin name and function name are both set to `functionName`. `IAgenticExecutionService` and `IPluginRegistry` are resolved lazily from `IServiceProvider` in the factory closure.
- `SubAgentPlugin` — invokable child agent. Reads `PluginExecutionContext` values at call time. Emits a `child-start` event (via `ChildStreamStart`) with the function name and prompt before invoking the child. Resolves the child `BehaviorProfile` by `profileId`, resolves child plugins via `IPluginRegistry`, constructs an `AgenticRequest` with `Depth + 1` and streaming callbacks wrapping `ChildStreamChunk`/`ChildStreamThinking` with the function name as the source. Calls `IAgenticExecutionService.ExecuteAsync` and returns the child response content as a string, or an error string if the endpoint is null or the profile is not found.
- `PluginExecutionContext` — static class with `AsyncLocal` properties: `CurrentEndpoint` (`ProviderEndpoint?`), `CurrentDepth` (`int`), `ChildStreamChunk` (`Func<string, string, Task>?`), `ChildStreamThinking` (`Func<string, string, Task>?`), `ChildStreamStart` (`Func<string, string, Task>?`). Set by `SemanticKernelAgenticExecutionService` from the incoming `AgenticRequest` at the start of each `ExecuteAsync` call. Flows through async continuations into plugin calls.
- Named `HttpClient` registrations: `"qbittorrent"` (plain), `"ollama"` (10-minute timeout, `OllamaThinkingHandler` then `OllamaLoggingHandler` attached in that order). `DefaultKernelFactory` uses the `"ollama"` client. `OllamaSharp` is a direct package reference in `CraterClaw.Core` (required to access `ChatResponseStream` for thinking token detection; the SK connector pulls it transitively but a direct reference is needed to use its types).
- `OllamaThinkingContext` — static class with `AsyncLocal<bool> ThinkingEnabled`. Set by `SemanticKernelAgenticExecutionService` before the agentic loop based on whether `StreamThinkingChunk` is non-null. Flows through async continuations into the HTTP handler.
- `OllamaThinkingHandler` — `DelegatingHandler` on the `"ollama"` client (outermost handler, runs before `OllamaLoggingHandler`). Reads the request body, parses it as JSON, and injects `"think": true` or `"think": false` based on `OllamaThinkingContext.ThinkingEnabled.Value`. This causes Ollama to skip the thinking phase entirely when thinking is disabled, rather than just hiding the tokens.

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
- `POST /api/providers/{name}/agentic/stream` — accepts `{ modelName, prompt, profileId, maxIterations?, showThinking? }`. Returns `text/event-stream`. SSE event types: `{"type":"chunk","content":"..."}` (parent model text), `{"type":"thinking","content":"..."}` (parent thinking, when `showThinking` is true), `{"type":"child-start","source":"...","prompt":"..."}` (child agent invoked with its prompt), `{"type":"child-chunk","source":"...","content":"..."}` (child model text streaming), `{"type":"child-thinking","source":"...","content":"..."}` (child thinking, when `showThinking` is true), `{"type":"done","finishReason":"...","toolsInvoked":[...]}`. SSE JSON is camelCase with string enum values. 404 / 400 on unknown provider / profile (set before headers are sent).

`AgenticRequest` fields: `ModelName`, `Prompt`, `Plugins`, `MaxIterations`, `StreamChunk` (`Func<string, Task>?`), `StreamThinkingChunk` (`Func<string, Task>?`), `SystemPrompt` (nullable string), `MaxContext` (nullable int), `Depth` (int, default 0). `SemanticKernelAgenticExecutionService` enforces `MaxChildAgentDepth = 2`; requests with `Depth >= MaxChildAgentDepth` return an error result immediately without invoking the model. `MaxContext` maps to Ollama's `num_ctx` — applied via `OllamaPromptExecutionSettings.ExtensionData["num_ctx"]` when non-null. Enums are serialized as strings (`JsonStringEnumConverter` applied globally). Internal types are visible to `CraterClaw.Api.Tests` via `InternalsVisibleTo`.

## CraterClaw.Web

Vue 3 TypeScript frontend (Vite, Vitest). Consumes `CraterClaw.Api` over HTTP. API base URL read from `VITE_API_BASE_URL` environment variable (defaults to `http://localhost:5000`).

### Project Layout

- `src/api/types.ts` — shared TypeScript types mirroring all API response shapes.
- `src/api/client.ts` — typed `fetch` wrappers for all API endpoints.
- `src/composables/` — Vue composables for stateful data fetching.
- `src/components/` — reusable UI components (populated in later phases).
- `src/App.vue` — root component; wires composables to UI.

### Implemented

- `getProviders`, `getProviderStatus`, `getModels`, `postAgentic`, `streamAgentic` in `client.ts`.
- `useProviders` composable: fetches provider list, tracks selected provider, fetches and exposes status.
- `useModels` composable: fetches models for selected provider, tracks selected model.
- `useProfiles` composable: fetches profile list, tracks selected profile.
- `useAgentic` composable: wraps `streamAgentic`; exposes `content`, `thinking` (builds up from thinking events), `showThinking` (boolean ref, default false; when true, `run` adds `showThinking: true` to the request), `finishReason`, `toolsInvoked`, `loading`, `error`, `run(providerName, request)`, and `cancel()`. Used by `AgenticPanel`.
- `useBehaviorDefaults` composable: takes `providers` and `models` refs and `selectProvider`/`selectModel` callbacks. `applyProfileDefaults(profile)` applies preferred provider/model defaults from the profile, calling the appropriate select function if the preferred value is found, or pushing a warning string to `behaviorWarnings` if not. Warnings are cleared on each call.
- `ProfileSelector` component: numbered list of profiles with name and description.
- `AgenticPanel` component: task prompt input with a "show thinking" checkbox. Displays thinking tokens in an expanded `<details>` block when present. Displays child agent outputs as expanded `<details>` blocks labeled with the function name, each showing the prompt sent to the child and its streamed response. Displays response content, finish reason, and tools invoked list. All three scrollable areas (thinking, child output, response) auto-scroll to the bottom during streaming and stop auto-scrolling when the user manually scrolls up; auto-scroll resumes on the next run.
- `AppTaskbar` component: persistent top taskbar with three expandable dropdown selectors (profile, provider, model). Profile selector is visually prominent (left accent border). Provider selector shows an inline reachability pill. Model selector is disabled when no provider is selected. Behavior warnings render below the taskbar row. Emits `selectProvider`, `selectModel`, `selectProfile`.
- `App.vue`: two-zone layout — `AppTaskbar` pinned at the top, agentic panel in the main content area. Shows a placeholder when no provider or model is selected. When a profile is selected, `applyProfileDefaults` is called to apply preferred provider/model and surface warnings into the taskbar.

### API Types (`src/api/types.ts`)

- `ProviderEndpoint`, `ProviderStatus`, `ModelItem` — mirror the provider API responses.
- `BehaviorProfile` — `id`, `name`, `description`, `systemPrompt`, `preferredProviderName` (null or string), `preferredModelName` (null or string), `plugins` (array of `PluginBinding`).
- `PluginBinding` — `name`, `tools` (string array).
- `AgenticRequest`, `AgenticResponse`, `AgenticSseChunk`, `AgenticSseThinking`, `AgenticSseDone`, `AgenticSseEvent` — agentic execution request/response and SSE event types.

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
