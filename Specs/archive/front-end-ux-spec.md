# Front-End UX Spec

## Name
- Front-End UX

## Checkpoint
- front-end-ux

## Depends On
- vue-frontend (checkpoint 12)

## Purpose
Redesign the Vue frontend from a plain functional layout into a cohesive, refined dark workspace. The aesthetic is a unified monospace control panel — precise, structured, and unmistakably personal.

## Design System

### Aesthetic Direction

Every element uses a single monospace typeface (DM Mono) giving the interface a coherent terminal-OS character. A second display font (Syne) is used exclusively for the wordmark. The dark blue-black palette has one electric-blue accent that appears only where it matters: the active selection indicator, interactive controls, and status highlights. Sections reveal progressively as prerequisites are met, giving the workflow a deliberate, cinematic quality.

### Fonts

Load via `index.html` `<head>`:

```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=DM+Mono:ital,wght@0,300;0,400;0,500;1,300&family=Syne:wght@800&display=swap" rel="stylesheet">
```

- **DM Mono** — used for all UI text, labels, inputs, responses, and metadata.
- **Syne 800** — used exclusively for the "CRATERCLAW" header wordmark.

### CSS Custom Properties

Defined on `:root` in `App.vue` `<style>`:

```css
:root {
  --bg:            #090d16;
  --surface:       #0f1624;
  --surface-raised:#16202f;
  --border:        #1e2d45;
  --border-active: #3d6abf;
  --text:          #c4d4eb;
  --text-dim:      #4e6480;
  --text-placeholder: #2e4260;
  --accent:        #4f8ef7;
  --accent-hover:  #6fa5ff;
  --ok:            #3ecf78;
  --err:           #e05555;
  --font-ui:       'DM Mono', monospace;
  --font-display:  'Syne', sans-serif;
  --radius:        6px;
  --transition:    140ms ease;
}
```

### Global Resets

```css
*, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
body {
  background: var(--bg);
  color: var(--text);
  font-family: var(--font-ui);
  font-size: 13px;
  line-height: 1.6;
  -webkit-font-smoothing: antialiased;
}
```

### Selection Pattern

The active/selected state for any list item (provider, model, profile) is expressed with a 3px left border in `--accent` and a `--surface-raised` background. Unselected items have a transparent left border (3px solid transparent) to prevent layout shift on selection.

### Panel Pattern

Each workflow section is a panel:

```css
.panel {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  padding: 20px 24px;
}
.panel-label {
  font-size: 10px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--text-dim);
  margin-bottom: 12px;
}
```

---

## Phases

### Phase 1: Design System + App Layout

**Status: Done**

#### Scope

- Add Google Fonts link to `index.html`.
- Establish CSS custom properties and global resets in `App.vue`.
- Redesign `App.vue` template: wordmark header, main workflow column, panel wrappers for each section.
- Implement provider list and status indicator styling.
- Implement model list styling.

#### Contract

`App.vue` structural layout:

```
#app
  header.app-header
    h1.wordmark  "CRATERCLAW"
  main.workspace
    section.panel  [providers]
    section.panel  [models — conditionally rendered]
    section.panel  [chat — conditionally rendered]
    section.panel  [profiles — conditionally rendered]
    section.panel  [agentic — conditionally rendered]
```

**Header**: `h1.wordmark` uses `font-family: var(--font-display)`, `font-size: 22px`, `letter-spacing: 0.05em`, `color: var(--text)`. No decoration. Header has `padding: 32px 0 24px` and sits at the top of the centered column.

**Workspace column**: `max-width: 680px`, `margin: 0 auto`, `padding: 0 24px 48px`, sections separated by `14px` gap using `display: flex; flex-direction: column; gap: 14px`.

**Provider list items**:
- Each `<li>` has `padding: 8px 12px`, `border-radius: var(--radius)`, `cursor: pointer`, `border-left: 3px solid transparent`, `transition: background var(--transition), border-color var(--transition)`.
- Hover: `background: var(--surface-raised)`.
- Selected: `border-left-color: var(--accent)`, `background: var(--surface-raised)`.
- Index number in `var(--text-dim)`, name in `var(--text)`, base URL in `var(--text-dim)` at `font-size: 11px`.

**Status indicator**: inline pill next to the selected provider name. `font-size: 11px`, `padding: 2px 8px`, `border-radius: 999px`. Reachable: `background: rgba(62,207,120,0.12)`, `color: var(--ok)`. Unreachable: `background: rgba(224,85,85,0.12)`, `color: var(--err)`.

**Model list items**: same selection pattern as providers. Display model name and size (formatted as MB/GB) on one line. `modifiedAt` omitted from the UI.

**Section conditional rendering**: unchanged logic — same `v-if` conditions as today. Only the wrapping and styling changes.

#### Tests

No new logic; existing composable and component tests must continue to pass without modification.

#### README Sync

No README changes required for this phase.

#### Current Architecture Sync

No architecture changes; this phase is purely visual.

#### Manual Verification Plan

1. Run `npm run dev` in `CraterClaw.Web`.
2. Confirm the wordmark "CRATERCLAW" renders in Syne 800 at the top of the page.
3. Confirm the page background matches `#090d16`.
4. Select a provider — confirm the left-border accent appears on the selected item.
5. Confirm the status pill appears with correct color (green/red) after status check.
6. Select a model — confirm the same selection pattern applies.

---

### Phase 2: Component Redesign

**Status: Done**

#### Scope

Restyle `InteractiveChat.vue`, `ProfileSelector.vue`, and `AgenticPanel.vue` to match the design system. No logic or prop changes.

#### Contract

**InteractiveChat**

Message history area:
- `min-height: 200px`, `max-height: 480px`, `overflow-y: auto`.
- Each message is a row with a role label column and content column.
- Role label: `width: 72px`, `flex-shrink: 0`, `color: var(--text-dim)`, `font-size: 11px`, `text-transform: uppercase`, `letter-spacing: 0.08em`, `padding-top: 2px`. Value is `"you"` or `"assistant"`.
- Content: `color: var(--text)`, `font-size: 13px`, `white-space: pre-wrap`. Assistant messages have `color: var(--text)`. User messages have `color: var(--text)` with no visual distinction beyond the label.
- Messages separated by a 1px `var(--border)` bottom border, last child has no border.
- Empty state: `"no messages"` in `var(--text-dim)`, centered vertically and horizontally within the history area.

Input bar (pinned to bottom of component):
- `display: flex`, `gap: 8px`, `padding-top: 12px`, `border-top: 1px solid var(--border)`.
- `<textarea>` (single row, expands to 3 rows max): `background: var(--surface-raised)`, `border: 1px solid var(--border)`, `border-radius: var(--radius)`, `color: var(--text)`, `font-family: var(--font-ui)`, `font-size: 13px`, `padding: 8px 12px`, `resize: none`, `flex: 1`. Focus: `border-color: var(--border-active)`, `outline: none`.
- Send button: `background: var(--accent)`, `color: #fff`, `border: none`, `border-radius: var(--radius)`, `padding: 8px 16px`, `font-family: var(--font-ui)`, `font-size: 12px`, `cursor: pointer`, `letter-spacing: 0.04em`. Hover: `background: var(--accent-hover)`. Disabled: `opacity: 0.4`, `cursor: not-allowed`.
- Submit on `Enter` (without Shift); `Shift+Enter` inserts a newline. Change the input from `<input>` to `<textarea>` — update component logic to match.

**ProfileSelector**

- Remove the `<ol>` pattern. Render profiles as a vertical list of selectable rows (same selection pattern as providers/models).
- Each row: index number, profile name, description on the same line separated by ` — `, description in `var(--text-dim)`.
- Selected row has the left-border accent.

**AgenticPanel**

- Task input: same `<textarea>` + button pattern as InteractiveChat. Button label `"run"`. Submit on `Enter` (no Shift).
- Result area (shown after execution):
  - Finish reason: `font-size: 11px`, `text-transform: uppercase`, `letter-spacing: 0.08em`, `color: var(--text-dim)`, `margin-bottom: 8px`.
  - Tools invoked: rendered as a single line of comma-separated names in `var(--accent)` font size `11px`. If none, omit entirely.
  - Response content: `background: var(--surface-raised)`, `border: 1px solid var(--border)`, `border-radius: var(--radius)`, `padding: 16px`, `white-space: pre-wrap`, `font-size: 13px`, `line-height: 1.7`, `max-height: 480px`, `overflow-y: auto`.

#### Tests

Existing component tests must pass. Update any selector-based queries that break due to element type changes (e.g. `input` → `textarea` in InteractiveChat and AgenticPanel).

#### README Sync

No changes required.

#### Current Architecture Sync

No changes required.

#### Manual Verification Plan

Prerequisites: API running, provider reachable, model selected.

1. Open the chat panel. Confirm message history area, role labels, and input textarea render correctly.
2. Type a message, press Enter to send. Confirm message appends and response appears.
3. Press Shift+Enter in the textarea. Confirm a newline is inserted rather than submitting.
4. Select a profile. Confirm the row selection pattern matches providers/models.
5. Run an agentic task. Confirm tools invoked line renders in accent color and response appears in the raised surface box.

---

### Phase 3: Transitions and Loading States

**Status: Done**

#### Scope

Add CSS transitions for panel reveal, selection state changes, and loading feedback. No logic changes.

#### Contract

**Panel reveal**: Sections that appear conditionally (`v-if`) use Vue's `<Transition>` with `name="panel"`:

```css
.panel-enter-active { transition: opacity 180ms ease, transform 180ms ease; }
.panel-enter-from   { opacity: 0; transform: translateY(6px); }
```

**Loading state in InteractiveChat**: while `loading` is true, show a pulsing `"..."` line in the message history in place of the pending assistant message. Three dots animate opacity in sequence using CSS `@keyframes`.

**Loading state in AgenticPanel**: while `loading` is true, the result area shows a single pulsing line (`"running..."`) in `var(--text-dim)`.

**Button active state**: `transform: scale(0.97)` on `:active` for send/run buttons.

#### Tests

No test changes required.

#### README Sync

No changes required.

#### Current Architecture Sync

No changes required.

#### Manual Verification Plan

1. With the API running and provider reachable, reload the page. Confirm provider panel is visible immediately.
2. Select a provider. Confirm the model panel fades and slides in smoothly.
3. Send a chat message. Confirm the animated `"..."` indicator appears before the response.
4. Run an agentic task. Confirm `"running..."` appears during execution.
5. Click the send button rapidly. Confirm the scale-down active state is visible.
