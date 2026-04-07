# Spec: blazor-components

**Checkpoint:** blazor-components
**Branch:** spec/blazor-components
**Type:** Code

## Goal

Replace the native `<select>` elements in `CraterClaw.Blazor` with custom Blazor components that match the Vue app's taskbar UX: expandable dropdown selectors, inline provider status pill, profile accent border, and description text in option rows. Then retire `CraterClaw.Web` and `CraterClaw.Api`.

---

## Phase 1: AppTaskbar component

**Status:** Done

### Contract

New file: `CraterClaw.Blazor/Components/AppTaskbar.razor`

Parameters:
```csharp
[Parameter] public List<string> Providers { get; set; } = [];
[Parameter] public string? SelectedProvider { get; set; }
[Parameter] public ProviderStatus? ProviderStatus { get; set; }

[Parameter] public List<ModelDescriptor> Models { get; set; } = [];
[Parameter] public string? SelectedModel { get; set; }

[Parameter] public List<BehaviorProfile> Profiles { get; set; } = [];
[Parameter] public string? SelectedProfile { get; set; }

[Parameter] public List<string> Warnings { get; set; } = [];

[Parameter] public EventCallback<string> OnProviderSelected { get; set; }
[Parameter] public EventCallback<string> OnModelSelected { get; set; }
[Parameter] public EventCallback<string> OnProfileSelected { get; set; }
```

Behaviour:
- One `openSection` field (`"profile"`, `"provider"`, `"model"`, or `null`). Only one dropdown open at a time.
- Clicking a trigger toggles its section; clicking the overlay (full-screen transparent div rendered when any section is open) closes all.
- Profile trigger has an accent left border (`selector-trigger--profile`). Options show name + description.
- Provider trigger shows an inline status pill when a provider is selected: `ok` (green) when reachable, `err` (red) when not. Options show name + base URL.
- Model trigger is disabled when `SelectedProvider` is null. Options show name only. Dropdown is right-aligned.
- Warnings render below the taskbar row when non-empty.

`Home.razor` updated to:
- Remove the `<header>` block and its state/logic.
- Inject the same services it already has.
- Render `<AppTaskbar>` at the top, passing current state and wiring the `On*Selected` callbacks back into the existing `OnProviderChanged`, model select, and `OnProfileChanged` methods.

### Tests

No automated tests. Existing suite must pass.

### Implement

1. Create `Components/AppTaskbar.razor` with the structure above.
2. Add CSS for the custom dropdown to `wwwroot/css/app.css`: `.selector`, `.selector-trigger`, `.selector-trigger--profile`, `.selector-trigger--active`, `.selector-trigger:disabled`, `.selector-label`, `.selector-value`, `.selector-chevron`, `.selector-dropdown`, `.selector-dropdown--right`, `.selector-list`, `.selector-option`, `.selector-option--selected`, `.option-name`, `.option-meta`, `.overlay`.
3. Remove the `<header>` markup from `Home.razor`. Replace with `<AppTaskbar ... />`. Remove the select-related fields from `Home.razor` that move to the component (open state, overlay). Keep provider/model/profile state fields in `Home.razor` — the taskbar is display-only and fires callbacks.
4. Remove the old `.selectors`, `.selector-group`, `select`, and `.status-pill` CSS rules from `app.css` that are superseded by the new component styles.

### Manual Verification Plan

1. Run `CraterClaw.Blazor`. Taskbar renders with CRATERCLAW wordmark and three selector triggers.
2. Click the profile trigger — dropdown opens with name + description per row. Click a profile — dropdown closes, trigger shows selected name.
3. Click the provider trigger — dropdown opens. Select a provider — status pill appears on the trigger (ok/err). Click provider trigger again — dropdown opens, selected provider has accent left border.
4. Model trigger is disabled until a provider is selected. After selecting a provider, model trigger enables and models load.
5. Select a profile with preferred provider/model — both selectors update automatically, warnings appear below the taskbar if values are missing.
6. Click outside an open dropdown — it closes.
7. Agentic panel still streams correctly after the refactor.

