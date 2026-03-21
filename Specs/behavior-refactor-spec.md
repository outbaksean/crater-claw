# Spec: behavior-refactor

## Goal

Move behavior definitions out of hardcoded C# into `craterclaw.json`. Each behavior gains a system prompt, preferred model and provider defaults, and a list of plugins with optional per-tool filtering. When a behavior is selected, the preferred provider and model are applied automatically if available; if not, a warning is shown. A plugin registry maps plugin/tool names to pre-filtered SK kernel plugins, replacing the hardcoded plugin-selection logic.

## Checkpoint Deliverable

- Behaviors are defined in `craterclaw.json` under a `behaviors` section.
- Each behavior specifies plugins as a list of `{ name, tools }` entries; empty `tools` means all tools in the plugin.
- `BehaviorProfile` includes `SystemPrompt`, `PreferredModelName` (nullable), `PreferredProviderName` (nullable), and `Plugins` (list of plugin bindings with tool allowlists).
- Selecting a behavior pre-selects its preferred provider and model where available; shows a warning where not.
- The agentic execution service applies the behavior's system prompt.
- A plugin registry resolves plugin bindings into pre-filtered SK `KernelPlugin` instances.
- Hardcoded behavior catalog and hardcoded plugin-selection logic are removed.
- Web frontend handles behavior-driven provider/model defaults with warning states.

---

## Phase 1: Behavior config types and profile model update

**Status:** Pending

### Contract

New config option types bound to the `behaviors` section of `craterclaw.json`:

```csharp
public sealed class PluginEntry
{
    public string Name { get; set; } = "";
    public List<string> Tools { get; set; } = []; // empty = all tools in the plugin
}

public sealed class BehaviorEntry
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string SystemPrompt { get; set; } = "";
    public string? PreferredProviderName { get; set; }
    public string? PreferredModelName { get; set; }
    public List<PluginEntry> Plugins { get; set; } = [];
}

// Bound to config section "behaviors" as Dictionary<string, BehaviorEntry>, keyed by behavior id.
```

Updated `BehaviorProfile` record:

```csharp
public sealed record PluginBinding(string Name, IReadOnlyList<string> Tools);

public sealed record BehaviorProfile(
    string Id,
    string Name,
    string Description,
    string SystemPrompt,
    string? PreferredProviderName,
    string? PreferredModelName,
    IReadOnlyList<PluginBinding> Plugins);
```

`IBehaviorProfileService` interface is unchanged. `BehaviorProfileService` loads from config instead of hardcoded catalog. `RecommendedModelTags` and `AllowedMcpServerNames` are removed from `BehaviorProfile`.

### Tests (write before implementation)

Update `BehaviorProfileServiceTests`:

- Construct the service with a populated config (two entries); verify `GetAll` returns both.
- Verify `GetById` returns the correct profile (case-insensitive).
- Verify `GetById` returns null for an unknown id.
- Verify `SystemPrompt`, `PreferredProviderName`, `PreferredModelName` are mapped correctly.
- Verify null preferred fields pass through as null.
- Verify `Plugins` list maps correctly: name and tools list preserved; empty tools list preserved as empty.
- Verify empty config produces an empty catalog.

Update `ProfilesMcpAgenticEndpointTests` to reflect the changed `BehaviorProfile` shape returned by `GET /api/profiles`.

### Implementation

1. Add `PluginEntry` and `BehaviorEntry` to `CraterClaw.Core`.
2. Add `PluginBinding` record to `CraterClaw.Core`.
3. Register `Dictionary<string, BehaviorEntry>` via `IOptions<>` in `ServiceCollectionExtensions`, bound to `"behaviors"`.
4. Rewrite `BehaviorProfileService` to inject options and map entries to `BehaviorProfile` records (dict key becomes `Id`; `PluginEntry` → `PluginBinding`).
5. Remove `RecommendedModelTags` and `AllowedMcpServerNames` from `BehaviorProfile` and the hardcoded catalog from `BehaviorProfileService`.
6. Add sample behavior definitions to `craterclaw.json` in `CraterClaw.Console`:

```json
"behaviors": {
  "no-tools": {
    "name": "No Tools",
    "description": "General-purpose conversation with no external tools.",
    "systemPrompt": "You are a helpful assistant.",
    "preferredProviderName": null,
    "preferredModelName": null,
    "plugins": []
  },
  "qbittorrent-manager": {
    "name": "qBitTorrent Manager",
    "description": "Querying and managing downloads using qBitTorrent.",
    "systemPrompt": "You are a torrent management assistant. Use the available tools to help manage downloads.",
    "preferredProviderName": null,
    "preferredModelName": null,
    "plugins": [
      { "name": "qbittorrent", "tools": [] }
    ]
  }
}
```

### Manual Verification Plan

- `dotnet test` — all tests pass.
- Run console harness; select a behavior; verify name and description display.
- `GET /api/profiles` returns both behaviors with `systemPrompt`, `preferredProviderName`, `preferredModelName`, and `plugins` (array of `{ name, tools }`).

---

## Phase 2: System prompt in agentic execution

**Status:** Pending

### Contract

`AgenticRequest` gains a `SystemPrompt` field:

```csharp
public sealed record AgenticRequest(
    string EndpointBaseUrl,
    string ModelName,
    string Prompt,
    string? SystemPrompt,
    IReadOnlyList<KernelPlugin> Plugins,
    Action<string>? StreamChunk = null,
    int MaxIterations = 10);
```

Note: `Plugins` type is `IReadOnlyList<KernelPlugin>` in anticipation of Phase 3 (the registry will return pre-built `KernelPlugin` instances). If this creates a compilation dependency, keep `IReadOnlyList<object>` in Phase 2 and change it in Phase 3 — whichever is cleaner at implementation time.

`SemanticKernelAgenticExecutionService` prepends a system message to the chat history when `SystemPrompt` is non-null and non-empty.

### Tests (write before implementation)

Add to `SemanticKernelAgenticExecutionServiceTests` (create file if absent):

- When `SystemPrompt` is set, the first message in chat history is a system role message with that content.
- When `SystemPrompt` is null or empty, no system message is prepended.

Tests must not hit a real Ollama instance; mock or stub the SK chat completion.

### Implementation

1. Add `SystemPrompt` (nullable string) to `AgenticRequest`.
2. In `SemanticKernelAgenticExecutionService.ExecuteAsync`, after constructing `ChatHistory`, call `chatHistory.AddSystemMessage(request.SystemPrompt)` if the value is non-null and non-empty.
3. Update the console to pass `profile.SystemPrompt` in `AgenticRequest`.
4. Update the API agentic endpoint to pass `profile.SystemPrompt` in `AgenticRequest`.

### Manual Verification Plan

- Run console; pick qbittorrent-manager; issue a task prompt. With `aiLogging.enabled: true`, confirm the system prompt appears in the AI traffic log.
- `dotnet test` — all tests pass.

---

## Phase 3: Plugin registry with tool filtering, and preferred defaults in console and API

**Status:** Pending

### Contract

New types in `CraterClaw.Core`:

```csharp
public interface IPluginRegistry
{
    // Returns pre-filtered KernelPlugin instances ready to add to a kernel.
    // Empty Tools list in a PluginBinding means all tools in the plugin are included.
    // Unknown plugin names are skipped (logged as warning).
    IReadOnlyList<KernelPlugin> Resolve(IEnumerable<PluginBinding> plugins);
}
```

`DefaultPluginRegistry` implements `IPluginRegistry`. At construction it holds a map of plugin name → plugin factory/instance (injected via DI). For each `PluginBinding`:

1. Look up the raw plugin instance by name. If not found, log a warning and skip.
2. Create a full `KernelPlugin` from the instance via `KernelPluginFactory.CreateFromObject`.
3. If `PluginBinding.Tools` is non-empty, filter the plugin's functions to only those whose names appear in the `Tools` list (case-insensitive). Unknown tool names are logged as warnings and skipped. Build a new `KernelPlugin` from the filtered functions via `KernelPluginFactory.CreateFromFunctions`.
4. If `PluginBinding.Tools` is empty, use the full plugin unfiltered.

`AgenticRequest.Plugins` changes to `IReadOnlyList<KernelPlugin>`. `SemanticKernelAgenticExecutionService` uses `kernel.Plugins.Add(plugin)` for each entry instead of `kernel.Plugins.AddFromObject(plugin)`.

**Preferred default resolution — console:**
After the user selects a behavior:

- If `profile.PreferredProviderName` is non-null: check whether it matches a configured endpoint name. If found and different from the current selection, switch to it (re-fetch models) and note `"Behavior prefers provider: {name}"`. If not found, print `"Warning: behavior prefers provider '{name}' which is not configured"` and continue.
- If `profile.PreferredModelName` is non-null: check whether it is in the model list for the active provider. If found, use it and note `"Behavior prefers model: {name}"`. If not found, print `"Warning: behavior prefers model '{name}' which is not available at this provider"` and continue with the current model.

**Preferred default resolution — API:**
The API agentic endpoint accepts explicit `ModelName` and `ProviderName` from the caller. Resolution is a client-side concern; no server-side preferred-default logic needed.

### Tests (write before implementation)

`PluginRegistryTests`:

- Resolving a binding for `"qbittorrent"` with empty tools returns a `KernelPlugin` containing all qBitTorrent functions.
- Resolving a binding for `"qbittorrent"` with `tools: ["ListTorrents"]` returns a `KernelPlugin` containing only `ListTorrents`.
- Resolving a binding with tools that include an unknown function name skips unknowns; known functions are still included.
- Resolving an empty binding list returns an empty list.
- Resolving a binding with an unknown plugin name returns an empty list (no exception).
- Resolving a mix of known and unknown plugin names returns only the known ones.

### Implementation

1. Create `IPluginRegistry` and `DefaultPluginRegistry` in `CraterClaw.Core`. Inject `QBitTorrentPlugin` (or its factory) and map `"qbittorrent"` to it.
2. Implement tool filtering using `KernelPluginFactory.CreateFromObject` and `KernelPluginFactory.CreateFromFunctions`.
3. Register `IPluginRegistry` → `DefaultPluginRegistry` as singleton in `ServiceCollectionExtensions`.
4. Update `AgenticRequest.Plugins` to `IReadOnlyList<KernelPlugin>`. Update `SemanticKernelAgenticExecutionService` to use `kernel.Plugins.Add(plugin)`.
5. Console: replace plugin-selection block with `pluginRegistry.Resolve(profile.Plugins)`. Implement preferred provider and model resolution.
6. API: replace plugin-selection block with `pluginRegistry.Resolve(profile.Plugins)`.
7. Fix any compilation failures from removed fields (`RecommendedModelTags`, `AllowedMcpServerNames`).

### Manual Verification Plan

**Dependencies:** `craterclaw.json` with both behavior entries; qBitTorrent running for the qbittorrent-manager path.

- `dotnet test` — all tests pass.
- Console: no-tools behavior → agentic task → AI makes no tool calls.
- Console: qbittorrent-manager with empty tools → all qBitTorrent functions available.
- Update qbittorrent-manager to `"tools": ["ListTorrents", "SearchTorrents"]` → agentic task → only those two functions are invocable; other qBitTorrent functions are absent.
- Set `preferredModelName` on a behavior to a model that exists → console notes the preference and uses it.
- Set `preferredModelName` to a non-existent model → console prints warning, uses prior model selection.
- Set `preferredProviderName` to a non-configured provider → console prints warning, uses current provider.
- API `POST .../agentic` with `profileId: "no-tools"` — succeeds, no tools invoked.
- API with `profileId: "qbittorrent-manager"` — succeeds.

---

## Phase 4: Web frontend — behavior-driven defaults and warnings

**Status:** Pending

### Contract

**Updated TypeScript types** (`api/types.ts`):

```typescript
interface PluginBinding {
    name: string;
    tools: string[];
}

interface BehaviorProfile {
    id: string;
    name: string;
    description: string;
    systemPrompt: string;
    preferredProviderName: string | null;
    preferredModelName: string | null;
    plugins: PluginBinding[];
}
```

**Behavior-driven selection logic** (in `App.vue` or a dedicated composable):

When a profile is selected:

1. Clear any existing behavior warnings.
2. If `profile.preferredProviderName` is non-null:
    - If a provider with that name exists in the loaded provider list: call `selectProvider()` with it (triggers status check and model reload).
    - If not found: store warning `"Behavior prefers provider '{name}' which is not configured"`.
3. After models are loaded for the active provider, if `profile.preferredModelName` is non-null:
    - If a model with that name exists in the model list: call `selectModel()` with it.
    - If not found: store warning `"Behavior prefers model '{name}' which is not available at this provider"`.
4. If the preferred provider changed in step 2, wait for models to reload before applying step 3. Use a one-time watcher on the models list.
5. If the preferred provider did not change, apply step 3 immediately against the current models list.

**Warning display:** Render warnings inline beneath the profile selector. Use muted text consistent with the existing design system — informational, not error styling.

### Tests (write before implementation)

Vitest unit tests (new spec file):

- Selecting a profile with a matching preferred provider calls `selectProvider` with that provider.
- Selecting a profile with a non-matching preferred provider stores a warning and does not change the selected provider.
- Selecting a profile with a matching preferred model (models already loaded) calls `selectModel` with that model.
- Selecting a profile with a non-matching preferred model stores a warning and does not change the selected model.
- Selecting a new profile clears warnings from the previous profile.

Tests use stub providers/models lists; no network calls.

### Implementation

1. Update `BehaviorProfile` and add `PluginBinding` in `api/types.ts`. Remove `recommendedModelTags` and `allowedMcpServerNames`.
2. Add `behaviorWarnings` ref (`string[]`) in the relevant composable or `App.vue`.
3. In the `onSelectProfile` handler:
    - Clear warnings.
    - Run preferred provider resolution; update selection or push warning.
    - Coordinate model resolution after provider/models settle.
4. Display `behaviorWarnings` beneath the profile selector.
5. Fix any TypeScript errors from removed fields.

### README Sync

- Configuration section: document the full `behaviors` JSON structure including the `plugins` array with per-tool filtering, and all other fields.
- Console Flow section: describe preferred provider/model default behavior and warning output.

### Current Architecture Sync

Update `current-architecture.md`:

- Behavior Profiles: config-driven loading, full `BehaviorProfile` and `PluginBinding` field descriptions, per-tool filtering semantics, preferred defaults and warning behavior, `IPluginRegistry`.
- Remove `RecommendedModelTags` and `AllowedMcpServerNames` references.
- Note `AgenticRequest.SystemPrompt` and `AgenticRequest.Plugins` type change to `IReadOnlyList<KernelPlugin>`.
- Web: describe `preferredProviderName`/`preferredModelName` resolution and warning display.

### Manual Verification Plan

**Dependencies:** Running API with behaviors configured in `craterclaw.json`; at least one provider reachable.

- Select a behavior with `preferredModelName` set to an available model → that model is automatically selected in the web UI.
- Select a behavior with `preferredModelName` set to a non-available model → warning appears below the profile panel; model selection unchanged.
- Select a behavior with `preferredProviderName` set to an available provider → provider switches, models reload; if `preferredModelName` is also set and available, it is then selected.
- Select a behavior with `preferredProviderName` set to a non-configured provider → warning appears; provider unchanged.
- Select a second behavior → warnings from the previous behavior are cleared.
- `npm run lint` — zero errors.
- `npm run test` (Vitest) — all tests pass.
