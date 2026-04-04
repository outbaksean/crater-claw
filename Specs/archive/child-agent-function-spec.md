# Spec: child-agent-function

**Checkpoint:** child-agent-function
**Branch:** spec/child-agent-poc
**Type:** Code

## Goal

Add `SubAgentPlugin` — a class that, when registered as a behavior plugin, gives the parent model the ability to invoke a child agentic loop against a named behavior profile. This is the building block for multi-model pipelines. No behavior wiring in this checkpoint.

## Design Notes

**Endpoint availability:** The `ProviderEndpoint` is known at `ExecuteAsync` call time but not at plugin registry construction time. A new `PluginExecutionContext` class (following the `OllamaThinkingContext` pattern) exposes the current endpoint via `AsyncLocal<ProviderEndpoint?>`. `SemanticKernelAgenticExecutionService` sets it before the loop.

**Depth enforcement:** `AgenticRequest` gains `int Depth = 0`. `SemanticKernelAgenticExecutionService` returns an error result (does not execute) if `Depth >= MaxChildAgentDepth` (hardcoded constant, value: `2`). `SubAgentPlugin` creates child requests with `Depth + 1`.

**Plugin wiring:** `SubAgentPlugin` is a plain C# class with `[KernelFunction]` methods. The `"subagent"` factory in `DefaultPluginRegistry` creates instances using services injected into the registry. This requires `ServiceCollectionExtensions` to inject `IBehaviorProfileService`, `IAgenticExecutionService`, and `IPluginRegistry` into the registry.

**No parallel execution:** `SubAgentPlugin` is invoked sequentially via SK's function call loop. No concurrent child calls are made.

---

## Phase 1: Hidden behavior profiles

**Status:** Done

### Scope

Add an optional `hidden` flag to behavior profiles. Hidden profiles are excluded from `GetAll()` (and therefore the API and UI) but remain resolvable by `GetById()` for use as child agent targets. This prevents child-only profiles from appearing in the UI selector.

### Contract

**`BehaviorEntry`** gains:

```csharp
public bool Hidden { get; set; }
```

**`BehaviorProfile`** gains:

```csharp
public bool Hidden { get; init; }
```

**`BehaviorProfileService`**:

- `GetAll()` returns only profiles where `Hidden == false`.
- `GetById(string id)` returns any profile regardless of `Hidden`.

`BehaviorProfileService` maps `BehaviorEntry.Hidden` → `BehaviorProfile.Hidden`.

### Tests

- `GetAll()` excludes profiles with `Hidden = true`.
- `GetAll()` includes profiles with `Hidden = false` (or unset, defaulting to false).
- `GetById()` returns a hidden profile by ID.
- `GetById()` returns a non-hidden profile by ID.

### Implement

1. Add `Hidden` to `BehaviorEntry`.
2. Add `Hidden` to `BehaviorProfile`.
3. Update `BehaviorProfileService` constructor to map the field.
4. Update `GetAll()` to filter `Hidden == false`.

### Manual Verification Plan

No user-visible change until `story-writing-behavior` adds hidden profiles. Verified by the tests passing.

---

## Phase 2: PluginExecutionContext and depth tracking

**Status:** Done

### Contract

**New `PluginExecutionContext`:**

```csharp
internal static class PluginExecutionContext
{
    public static AsyncLocal<ProviderEndpoint?> CurrentEndpoint { get; } = new();
    public static AsyncLocal<int> CurrentDepth { get; } = new();
}
```

**`AgenticRequest`** gains:

```csharp
int Depth = 0
```

**`SemanticKernelAgenticExecutionService`** sets both `AsyncLocal` values at the start of `ExecuteAsync`, before the loop. Returns a failed `AgenticResponse` immediately if `request.Depth >= MaxChildAgentDepth`.

```csharp
internal const int MaxChildAgentDepth = 2;
```

### Tests

- `ExecuteAsync` returns an error result when `request.Depth >= MaxChildAgentDepth` without calling the chat service.
- `ExecuteAsync` proceeds normally when `request.Depth < MaxChildAgentDepth`.

### Implement

1. Add `PluginExecutionContext` to `CraterClaw.Core`.
2. Add `Depth = 0` to `AgenticRequest`.
3. In `SemanticKernelAgenticExecutionService.ExecuteAsync`:
    - Add `MaxChildAgentDepth = 2` constant.
    - Return error `AgenticResponse` if `request.Depth >= MaxChildAgentDepth`.
    - Set `PluginExecutionContext.CurrentEndpoint.Value = endpoint`.
    - Set `PluginExecutionContext.CurrentDepth.Value = request.Depth`.

### Manual Verification Plan

No user-visible change. Verified by the tests passing.

---

## Phase 3: SubAgentPlugin and registry wiring

**Status:** Done

### Contract

**`SubAgentPlugin`:**

```csharp
internal sealed class SubAgentPlugin(
    string profileId,
    string functionName,
    string description,
    IBehaviorProfileService profileService,
    IAgenticExecutionService agenticService,
    IPluginRegistry pluginRegistry)
{
    [KernelFunction]
    [Description(/* set from description parameter */)]
    public async Task<string> RunAsync(string prompt, CancellationToken cancellationToken);
}
```

`RunAsync`:

1. Reads `PluginExecutionContext.CurrentEndpoint.Value` — returns an error string if null.
2. Resolves the child `BehaviorProfile` by `profileId` — returns an error string if not found.
3. Resolves child plugins via `IPluginRegistry`.
4. Creates `AgenticRequest` with `Depth = PluginExecutionContext.CurrentDepth.Value + 1`, child profile's `ModelName`, `SystemPrompt`, `MaxContext`, and the provided `prompt`.
5. Calls `IAgenticExecutionService.ExecuteAsync` and returns `AgenticResponse.Content`.

**`[KernelFunction]` name:** use `functionName` parameter. Because `[KernelFunction]` attribute names are set at compile time, this requires generating the plugin name dynamically. Use `KernelFunctionFactory.CreateFromMethod` with the method delegate and metadata override rather than the attribute approach, so the name is set at runtime.

**`DefaultPluginRegistry`** gains a `"subagent"` factory. Config keys: `profileId` (required), `functionName` (required), `description` (optional).

**`ServiceCollectionExtensions`** injects `IBehaviorProfileService`, `IAgenticExecutionService`, and `IPluginRegistry` into the `DefaultPluginRegistry` singleton registration.

> Note: `IAgenticExecutionService` is registered as `Transient`. The registry is `Singleton`. Resolve `IAgenticExecutionService` lazily via `IServiceProvider` passed to the registry rather than capturing it at construction time.

### Tests

- `SubAgentPlugin.RunAsync` returns an error string when `PluginExecutionContext.CurrentEndpoint` is null.
- `SubAgentPlugin.RunAsync` returns an error string when the profile is not found.
- `SubAgentPlugin.RunAsync` calls `IAgenticExecutionService.ExecuteAsync` with `Depth + 1` when context is valid.
- `SubAgentPlugin.RunAsync` returns the child `AgenticResponse.Content` on success.

### Implement

1. Add `SubAgentPlugin` to `CraterClaw.Core`.
2. Update `DefaultPluginRegistry` to accept a `"subagent"` factory using `IServiceProvider`.
3. Update `ServiceCollectionExtensions` to pass required services to the registry.

### README Sync

Add a `SubAgentPlugin` section to README explaining the `"subagent"` plugin type, its config keys (`profileId`, `functionName`, `description`), the max depth limit, and the convention that child agent profiles should not themselves include subagent bindings.

### Current Architecture Sync

Add `SubAgentPlugin`, `PluginExecutionContext`, and the `"subagent"` registry factory to `current-architecture.md`.

### Manual Verification Plan

No behavior uses `SubAgentPlugin` yet. Verify by running all tests. The next checkpoint (`story-writing-behavior`) provides end-to-end verification.
