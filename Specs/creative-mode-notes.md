# Creative Mode — Design Notes

**Status:** Exploratory — not yet broken into checkpoints

---

## The Core Idea

Add a "mode" concept to CraterClaw. A mode groups related behaviors and presents a UI tailored to that group's workflow. The default mode is the current UI. A "Creative" mode surfaces story-writing behaviors with a richer interface suited to iterative creative work.

---

## Modes

### Default Mode

The current UI. General-purpose agentic panel with profile/provider/model selection. No changes planned.

### Creative Mode

Focused on story writing and creative pipelines. The entry point is a story selector: the user either creates a new story or loads an existing one. From there, any agent in the roster can be run directly against the active story's vault.

Differences from the default UI:

- Entry point is New Story / Load Story rather than a generic task prompt.
- New Story takes structured inputs and deterministically scaffolds the Obsidian vault — no AI involved at this step.
- Only surfaces behaviors tagged as belonging to the creative mode. Hides others.
- All creative profiles are user-facing. The user can run any agent directly — a single specialist (e.g. Character Helper) or the Story Director which orchestrates child agents.
- Exposes editable system prompts and child agent definitions within the UI — so you can tune any agent's instructions without editing `craterclaw.json`.
- Agents read from and write to the story vault directly. The vault structure is fixed and hard-coded into agent system prompts; agents do not need to discover or infer the layout.
- Child agent prompt interception is toggleable. When enabled, the user can review and edit the prompt before each child agent is invoked. When disabled, the pipeline runs uninterrupted.

---

## Story Inputs

Collected on the New Story screen. Used only to populate the initial vault files — no agent is involved.

| Input | Required | Notes |
|---|---|---|
| Story Type | Yes | Short story, novel, etc. Determines default length. |
| Length | No | Word or page count. Defaults by story type, overridable. |
| Theme | No | |
| Characters | No | |
| Setting | No | |
| Plot | No | |

---

## Story Scaffolding

When the user creates a new story, the UI deterministically creates the Obsidian vault directory and its initial markdown files from the inputs above. This is a pure file-creation step — no model is invoked. The resulting structure is the source of truth the agents work from.

The exact vault layout is the output of `investigate-obsidian` and must be defined before any agent profiles can be fully specified. All agent system prompts will reference this structure by name (e.g. "read `characters/main.md` for the character roster"). The structure must be stable before agents are wired.

---

## Agent Profiles

All Creative Mode profiles are user-facing. The user can invoke any agent directly against the active story's vault, or run the Story Director which orchestrates the others as child agents.

| Profile | Role |
|---|---|
| Story Director | Orchestrates the pipeline; has access to all other agents as child agents |
| Context Helper | Retrieves only the vault data relevant to a given task; called before specialists to keep context lean |
| Character Helper | Develops and maintains character definitions |
| Plot Helper | Develops and maintains plot structure |
| Setting Helper | Develops and maintains setting and world details |
| Theme Helper | Works with theme, tone, and motif |
| Writer | Produces draft content |
| Reviewer | Reviews and critiques draft content |

The Context Helper pattern is important for local models: rather than passing the full vault to every agent, the Director first calls Context Helper to distill the relevant files, then passes that summary to the specialist. This keeps context windows lean.

When the Director runs, the typical invocation chain is:

```
Director -> Context Helper (retrieves relevant vault data) -> Specialist or Writer
```

This roster is TBD — see open design question 2.

---

## Open Design Questions

### 1. Story inputs (TBD)

The input list in the Story Inputs section is a starting point. The final set of fields — and which are required — depends on what the scaffolding step actually creates and what the agent system prompts expect to find in the vault. Revisit after `investigate-obsidian` defines the vault structure.

### 2. Agent profile roster (TBD)

The profiles listed in the Agent Profiles section are a working sketch. The final roster, what each agent reads and writes, and how the Director sequences them all depend on the vault structure. Do not finalize profiles or their system prompts until `investigate-obsidian` is done.

### 3. Where do modes live?

Options:

- **Config-defined** — `craterclaw.json` gains a `modes` section. Each mode has a name, a list of behavior IDs it surfaces, and a mode type that controls which Vue component renders it. Clean, extensible.
- **Hardcoded** — Default and Creative are just two Vue route/component pairs. No config needed. Simpler to build but harder to extend.
- **Hybrid** — Mode type is an enum on the behavior profile (e.g. `"mode": "creative"`). The UI routes on the first mode type it finds among selected profiles. Requires no separate modes config section but is less explicit.

The config-defined approach is cleanest architecturally. The hardcoded approach is fastest to build.

### 4. Editable system prompts and child agent definitions

Two sub-questions:

**Where is the source of truth?**

- UI-only (not persisted): edits are session-scoped. Lost on refresh. Easiest to implement.
- Persisted to user secrets or a local file: edits survive sessions. Requires a save mechanism.
- Persisted back to `craterclaw.json` via a settings panel: full round-trip. Most complex.

Starting with UI-only (session-scoped) is the right first step. Persistence is a separate checkpoint.

**What is editable?**

- System prompt of the director and each child agent.
- Child agent profile associations (which profile each function calls).
- Possibly: max context, preferred model per child.
- Not editable in UI (initially): plugin bindings, credentials.

### 5. File read/write tools

A `MarkdownFilePlugin` with kernel functions:

- `ReadFile(path)` — reads a file relative to a configured working directory.
- `WriteFile(path, content)` — writes or overwrites a file.
- `ListFiles(directory?)` — lists markdown files in the working directory.
- `AppendToFile(path, content)` — appends to an existing file (useful for incremental story sections).

The working directory is configured per behavior in `craterclaw.json` (similar to how qBitTorrent credentials are per-binding) and points to the story's Obsidian vault root. Because the vault structure is fixed and hard-coded in agent system prompts, agents reference files by known paths rather than discovering the layout at runtime.

Security consideration: path traversal. The plugin must validate that resolved paths stay within the configured working directory.

### 6. User-confirmation before child agent prompt

Child agent prompt interception is a toggleable feature. When off, the Director runs the full pipeline uninterrupted. When on, the user can review and edit each child prompt before it is dispatched. The toggle is per-run or per-session (TBD).

This is the most architecturally novel piece. Currently, the child agent prompt flows:

```
Director model -> function call with prompt arg -> SubAgentPlugin.RunAsync -> child ExecuteAsync
```

To insert a user confirmation step:

```
Director model -> function call -> SubAgentPlugin pauses -> UI shows prompt for review/edit -> user approves -> child ExecuteAsync
```

This requires a pause/resume mechanism across the async SSE stream. Options:

**Option A: SSE pause event + HTTP resume endpoint**

- `SubAgentPlugin` emits a `{"type":"child-pending","source":"PlanStory","prompt":"...","token":"<uuid>"}` SSE event and then blocks (e.g. waits on a `TaskCompletionSource`).
- The UI shows the prompt for editing and a confirm button.
- The user edits and clicks confirm, which POSTs `{"token":"<uuid>","prompt":"<edited>"}` to a new `/api/agentic/resume` endpoint.
- The server resolves the `TaskCompletionSource` with the (possibly edited) prompt.
- `SubAgentPlugin` continues with the confirmed prompt.

This keeps the SSE stream alive throughout and is architecturally clean but requires server-side state (a concurrent dictionary of pending tokens → TaskCompletionSources).

**Option B: Two-phase execution**

- Phase 1: run the director until it produces a function call, then stop. Return the pending call as part of the response.
- Phase 2: user edits the prompt, submits it. The pipeline resumes from the function call.

This avoids server-side blocking state but requires the director's chat history to be serializable and resumable. Complex with the current SK integration.

**Option C: Pre-run prompt editor (simpler)**

- Don't intercept at runtime. Instead, the Creative mode UI has an explicit "story pipeline" panel where the user defines the premise, then separately sets up what to send to the planner and the writer before running each.
- Loses the director model's autonomy (it no longer decides what to send to children) but gives the user full control.

Option A is the right answer for preserving the agentic model while adding user control. Option C is a simpler first step that doesn't require infrastructure changes.

---

## Proposed Checkpoint Sequence

These are rough — promote to `checkpoints.md` as they become concrete.

`blazor-poc` is done. The vault structure decision is the hard blocker: agent system prompts, the scaffolding step, and the `MarkdownFilePlugin` path conventions all depend on it. `investigate-obsidian` must come before any agent profile work.

1. **`investigate-obsidian`** — Define the vault directory structure and file naming conventions. Determine whether Obsidian-specific conventions (frontmatter, wikilinks) help or complicate things, and whether a plain filesystem plugin suffices or an Obsidian-aware plugin is needed. Output: the canonical vault layout, documented in this file, and a go/no-go decision on Obsidian as the storage layer.

2. **`mode-selection`** — Add a `mode` tag to behavior profiles in config. Mode selection in the UI filters the profile list to only show behaviors belonging to the selected mode. Foundation for all Creative Mode UX — no new UI panels yet, just the routing/filtering mechanism.

3. **`investigate-creative-ux`** — Define the Creative Mode UX checkpoints. After `investigate-obsidian` is done, review what the UI needs to do: New/Load story entry, scaffold step, editable system prompts and child agent definitions, file tool integration, child agent confirmation flow, Reviewer trigger pattern, and anything else that emerged. Output is a concrete checkpoint list added to `checkpoints.md`, not code.

---

## Relationship to Existing Architecture

- **`SubAgentPlugin`** would need extension for child-prompt-confirmation (Option A). The `PluginExecutionContext` pattern (AsyncLocal) could carry a confirmation callback, similar to how `ChildStreamChunk` works.
- **`MarkdownFilePlugin`** is a new entry in `DefaultPluginRegistry`. Config key: `workingDirectory`. Path conventions depend on the vault structure defined in `investigate-obsidian`.
- **Mode routing** in Vue: a new top-level composable or router that reads the active mode and renders either `AgenticPanel` (default) or a new `CreativePanel`.
- **Behavior tagging**: the simplest approach is a `"mode"` string field on `BehaviorEntry` / `BehaviorProfile`. The API would expose it; the Vue mode router reads it to decide which component to render for a given profile selection.
- **Story scaffolding**: a new UI-side action in `CreativePanel`. Calls a thin API endpoint or writes files directly (TBD based on deployment model). Does not invoke any model.
