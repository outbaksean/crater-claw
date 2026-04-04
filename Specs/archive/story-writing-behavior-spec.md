# Spec: story-writing-behavior

**Checkpoint:** story-writing-behavior
**Branch:** spec/child-agent-poc
**Type:** Code

## Goal

A proof-of-concept behavior that uses `SubAgentPlugin` to write a short story via a three-model pipeline:

- **Parent (`story-director`)**: `qwen3:14b`. Receives the user's premise, does creative direction work (expands the premise, establishes tone, genre, constraints), calls the planner child, then calls the writer child, and returns the finished story with a title.
- **Planner child (`story-planner`)**: `qwen3:8b` with thinking. Given the director's enriched premise, produces a structured outline: characters, setting, plot beats.
- **Writer child (`story-writer`)**: `gemma3:12b`. Given the outline plus the director's tone/constraints, writes the story prose.

This checkpoint is config-only: three new profiles in `craterclaw.json`, no C# changes.

---

## Phase 1: Story behavior profiles

**Status:** Done

### Scope

Add three behavior profiles to `craterclaw.json`. Verify the pipeline end-to-end in the web UI.

### Contract

No new C# types. Three new entries under `behaviors` in `craterclaw.json`:

**`story-planner`** (child — hidden, no plugins):

```json
"story-planner": {
  "name": "Story Planner",
  "description": "Creates a structured story outline from an enriched premise",
  "hidden": true,
  "systemPrompt": "You are a story structure specialist. Given a story premise with tone and constraints, produce a detailed outline: protagonist and supporting characters with brief sketches, setting, and 4-6 plot beats from opening to resolution. Be specific and concrete. Output only the outline.",
  "preferredProviderName": "local",
  "preferredModelName": "qwen3:8b",
  "maxContext": 4096,
  "plugins": []
}
```

**`story-writer`** (child — hidden, no plugins):

```json
"story-writer": {
  "name": "Story Writer",
  "description": "Writes short story prose from a structural outline",
  "hidden": true,
  "systemPrompt": "You are a prose writer. Given a story outline and creative direction, write the full short story. Write vivid, engaging prose. Stay true to the outline's structure and the specified tone. Output only the story text, no commentary.",
  "preferredProviderName": "local",
  "preferredModelName": "gemma3:12b",
  "maxContext": 8192,
  "plugins": []
}
```

**`story-director`** (parent — two subagent plugins):

```json
"story-director": {
  "name": "Story Director",
  "description": "Writes a short story using a planner and writer model",
  "systemPrompt": "You are a creative director for short fiction. When given a premise:\n1. Expand it: decide on tone (e.g. melancholy, tense, whimsical), genre, length, and one or two creative constraints that will make the story interesting.\n2. Call PlanStory with the enriched premise including your tone and constraints.\n3. Call WriteStory with the outline and your tone/constraints as context.\n4. Generate a title.\n5. Return the title followed by the complete story.\nDo not write the story yourself — use the tools.",
  "preferredProviderName": "local",
  "preferredModelName": "qwen3:14b",
  "maxContext": 16384,
  "plugins": [
    {
      "name": "subagent",
      "tools": [],
      "config": {
        "profileId": "story-planner",
        "functionName": "PlanStory",
        "description": "Creates a structured story outline from an enriched premise. Pass the premise with tone and creative constraints."
      }
    },
    {
      "name": "subagent",
      "tools": [],
      "config": {
        "profileId": "story-writer",
        "functionName": "WriteStory",
        "description": "Writes short story prose from a structural outline. Pass the outline and tone/constraints."
      }
    }
  ]
}
```

### Tests

No automated tests for config. Verified manually.

### Implement

Add the three profiles to `craterclaw.json`. Ensure `gemma3:12b` is downloaded in Ollama before manual verification.

### README Sync

Add a brief entry for the story-writing behaviors under the behavior profiles section.

### Current Architecture Sync

Add the three new profiles to the default profiles list in `current-architecture.md`.

### Manual Verification Plan

Dependencies: `ollama-context-config` and `child-agent-function` checkpoints complete. `gemma3:12b` downloaded in Ollama.

1. Start the app (`craterclaw run`).
2. Select the `local` provider and confirm it is reachable.
3. Select the `Story Director` profile — confirm `qwen3:14b` is applied as the preferred model.
4. Submit a short story premise (e.g. "a lighthouse keeper discovers a message in a bottle that changes everything").
5. Observe the agentic panel:
    - The director calls `PlanStory` (visible in tool invocations).
    - The director calls `WriteStory`.
    - The final response contains a title and story prose.
6. Confirm the story is coherent and reflects the premise.
7. Enable "show thinking" and re-run — confirm thinking tokens appear for the director (qwen3:14b) and planner (qwen3:8b) passes thinking through correctly.
8. Check the AI log — confirm three separate model invocations are logged (director, planner, writer).
