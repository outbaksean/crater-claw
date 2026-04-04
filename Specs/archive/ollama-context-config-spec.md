# Spec: ollama-context-config

**Checkpoint:** ollama-context-config
**Branch:** spec/child-agent-poc
**Type:** Code

## Goal

Allow behavior profiles to configure Ollama's context window size (`num_ctx`) per behavior. Addresses context degradation when the parent behavior in a child-agent pipeline accumulates a large chat history (premise + outline + story draft).

## Phase 1: MaxContext in behavior profiles

**Status:** Done

### Contract

**`BehaviorEntry`** gains:

```csharp
public int? MaxContext { get; set; }
```

**`BehaviorProfile`** gains:

```csharp
public int? MaxContext { get; init; }
```

**`AgenticRequest`** gains:

```csharp
int? MaxContext = null
```

`BehaviorProfileService` maps `BehaviorEntry.MaxContext` → `BehaviorProfile.MaxContext`.

**`OllamaPromptExecutionSettings`** — verify the exact property name for `num_ctx` against the installed `Microsoft.SemanticKernel.Connectors.Ollama` 1.73.0-alpha before implementation. Expected to be accessible via the settings object passed to `GetStreamingChatMessageContentsAsync` and `GetChatMessageContentsAsync`. If not directly available, use the `ExtensionData` dictionary with key `"num_ctx"`.

### Tests

- `BehaviorProfileService` maps `MaxContext` from config to `BehaviorProfile.MaxContext` when set.
- `BehaviorProfileService` produces `null` `MaxContext` when not configured.

### Implement

1. Add `MaxContext` to `BehaviorEntry`.
2. Add `MaxContext` to `BehaviorProfile`.
3. Update `BehaviorProfileService` to map the field.
4. Add `MaxContext` to `AgenticRequest`.
5. In `SemanticKernelAgenticExecutionService`, if `request.MaxContext` is set, apply it to `OllamaPromptExecutionSettings` before the loop.
6. Verify the property name against the package and update accordingly.

**API/Console:** both already pass `AgenticRequest` through; no other changes needed. Neither the API response shape nor the console UI changes.

### README Sync

Add `maxContext` (optional int) to the behavior profile config example and note that it maps to Ollama's `num_ctx` parameter. Recommend setting a large value (e.g. `8192` or `16384`) for behaviors that handle long outputs.

### Current Architecture Sync

Update `BehaviorEntry` / `BehaviorProfile` / `AgenticRequest` entries in `current-architecture.md`.

### Manual Verification Plan

- Add `"maxContext": 4096` to one behavior profile in `craterclaw.json`.
- Run the app, select that profile, send a prompt.
- Confirm the app runs without errors.
- Check the AI raw log (`aiRawLogging.enabled: true`) and verify `num_ctx` appears in the outgoing request JSON.
