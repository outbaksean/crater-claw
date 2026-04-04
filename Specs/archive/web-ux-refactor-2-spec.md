# Spec: web-ux-refactor-2

**Checkpoint:** web-ux-refactor-2
**Branch:** spec/web-ux-refactor-2
**Type:** Code

## Goal

Redesign the Vue frontend layout:

- A persistent top taskbar contains provider, model, and behavior profile selectors — always visible, always interactive.
- The agentic panel is the sole interaction mode and the main content of the page.
- Selecting a behavior profile silently applies its preferred provider and model; warns inline when preferred values are unavailable.
- Interactive mode (single-turn execution without a profile) is removed. It is superseded by the agentic panel with a no-tools profile.

## Current State Summary

`App.vue` renders a vertical column of panels that reveal progressively: provider → model → chat (interactive) → profile → task (agentic). Selections must happen in order. `InteractiveChat.vue` and `useExecution.ts` back the interactive chat panel. `AgenticPanel.vue` and `useAgentic.ts` back the agentic panel. `useBehaviorDefaults.ts` applies preferred provider/model when a profile is selected and produces a `behaviorWarnings` array.

## Target Layout

```
+--------------------------------------------------------------+
| CRATERCLAW   [profile selector v]  [provider v]  [model v]  |
|              [warning text if any]                           |
+--------------------------------------------------------------+
|                                                              |
|   Agentic panel (full content area)                         |
|                                                              |
+--------------------------------------------------------------+
```

- Taskbar spans full width; selections are always accessible.
- Profile selector is the leftmost and most prominent control.
- Provider selector shows an inline reachability pill next to the selected name.
- Model selector populates after provider is reachable.
- Behavior warnings appear below the taskbar row as a single line or list.
- The agentic panel input is disabled with a status message when no provider or model is selected.

---

## Phase 1: Remove interactive mode

**Status:** Done

### Scope

Delete the interactive execution path from the frontend. This covers `InteractiveChat.vue`, `useExecution.ts`, and their test files, plus removal of the interactive chat panel from `App.vue`.

The API's interactive execution endpoint is not touched — backend-only removal is a separate decision.

### Contract

No new types. Removals:

- `InteractiveChat.vue` and `InteractiveChat.spec.ts`
- `useExecution.ts` and `useExecution.spec.ts`
- `ExecutionRequest`, `ExecutionResponse`, `MessageItem` from `api/types.ts` (unused after removal)
- `client.ts` interactive execution method (if it becomes dead code — verify first)

### Tests

Delete `CraterClaw.Web/src/components/InteractiveChat.spec.ts` and `CraterClaw.Web/src/composables/useExecution.spec.ts`. Verify remaining tests still pass (`npm run test`).

### Implement

1. Delete `InteractiveChat.vue`.
2. Delete `useExecution.ts`.
3. In `App.vue`: remove the import, the `useExecution` usage, and the interactive chat `<section>` panel.
4. Remove `ExecutionRequest`, `ExecutionResponse`, `MessageItem` from `api/types.ts` if no remaining code references them.
5. Remove the interactive execution method from `api/client.ts` if it becomes dead code.

### README Sync

Update the Current State section to remove mention of interactive chat mode.

### Current Architecture Sync

Update `current-architecture.md` to reflect that interactive execution has been removed from the frontend.

### Manual Verification Plan

- Run `npm run test` — all tests pass.
- Run `npm run lint` — zero errors.
- Start the app. Confirm there is no interactive chat panel.
- Confirm the agentic panel still works end-to-end with a selected provider, model, and profile.

---

## Phase 2: Top taskbar layout

**Status:** Done

### Scope

Redesign `App.vue` into a two-zone layout: a persistent top taskbar containing all selectors, and a main content area containing only the agentic panel. Extract the taskbar into a `Taskbar.vue` component.

### Contract

**`Taskbar.vue` props:**

```ts
interface TaskbarProps {
    providers: ProviderEndpoint[];
    selectedProvider: ProviderEndpoint | null;
    providerStatus: ProviderStatus | null;
    loadingProviders: boolean;
    loadingStatus: boolean;

    models: ModelItem[];
    selectedModel: ModelItem | null;
    loadingModels: boolean;

    profiles: BehaviorProfile[];
    selectedProfile: BehaviorProfile | null;
    loadingProfiles: boolean;

    warnings: string[];
}
```

**`Taskbar.vue` emits:**

```ts
interface TaskbarEmits {
    selectProvider: [provider: ProviderEndpoint];
    selectModel: [model: ModelItem];
    selectProfile: [profile: BehaviorProfile];
}
```

**`App.vue`** becomes a layout shell: taskbar on top, agentic panel below. No selection logic lives in `App.vue` beyond wiring composables to `Taskbar` and `AgenticPanel`.

### Tests

`CraterClaw.Web/src/components/AppTaskbar.spec.ts`:

- Renders provider list with selected state.
- Renders model list with selected state; list is empty and disabled when no provider selected.
- Renders profile list with selected state.
- Emits `selectProvider` when a provider is clicked.
- Emits `selectModel` when a model is clicked.
- Emits `selectProfile` when a profile is clicked.
- Displays warnings when the `warnings` prop is non-empty.
- Shows provider reachability pill when a provider is selected and status is present.

### Implement

1. Create `CraterClaw.Web/src/components/Taskbar.vue` implementing the contract above. Layout: single horizontal row of three selector dropdowns/popovers or compact inline lists. Provider selector shows a reachability pill next to the selected provider name. Warnings render below the taskbar row.
2. Refactor `App.vue`:
    - Remove all panel markup.
    - Add a sticky/fixed top taskbar region containing `<Taskbar>`.
    - Add a main content region below the taskbar containing `<AgenticPanel>` (shown when provider and model are selected; otherwise a short placeholder).
    - Wire `useBehaviorDefaults` warnings into the `warnings` prop.
3. Adjust `AgenticPanel.vue` if its outer sizing assumptions change (it currently relies on being inside a `.panel` div).
4. Update global layout styles in `App.vue` — the current `max-width: 680px` column layout no longer applies; the taskbar spans full width and the content area uses a reasonable max-width for the chat.

### README Sync

Update the Current State section to describe the taskbar layout and removal of the panel-reveal flow.

### Current Architecture Sync

Update `current-architecture.md` to reflect the new layout structure and removal of `InteractiveChat`.

### Manual Verification Plan

- Run `npm run test` — all tests pass.
- Run `npm run lint` — zero errors.
- Start the app. Confirm the taskbar is visible at all times.
- Select a profile with a preferred provider and model — confirm they are silently applied.
- Select a profile whose preferred provider or model is not available — confirm a warning appears below the taskbar.
- Submit a prompt and confirm the agentic panel responds correctly.
- Resize the window — confirm the taskbar and content area remain usable.
