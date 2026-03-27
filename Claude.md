# CraterClaw

## Spec-Based Development

### Directory Structure

```text
Specs/
  target-architecture.md      -- end-goal architecture, high-level, rarely changes
  checkpoints.md              -- ordered list of working stages with dependencies
  current-architecture.md     -- living doc of what is actually built; updated each phase
  decisions.md                -- architectural decisions with rationale; pending decisions at the bottom
  {checkpoint-name}-spec.md   -- the one active spec; named after its checkpoint
  archive/                    -- completed specs moved here when their checkpoint is done
```

### Documents

- **target-architecture.md**: Describes the intended end state of the system at a high level. Updated only when the overall direction changes.
- **checkpoints.md**: An ordered list of working stages. Each checkpoint describes a verifiable, runnable state of the application. Dependencies between checkpoints are noted. The user maintains this list but may ask Claude to generate or update it.
- **current-architecture.md**: A technical description of what is currently built. Updated after every phase to reflect the real state of the code. Must be accurate enough that a new developer can understand the system without reading specs or source code.
- **decisions.md**: Records significant architectural decisions with rationale and the pending decisions that still need resolution. Update this file when a meaningful design decision is made during a session — not for implementation details, but for choices that would be hard to reconstruct later (technology selection, architectural tradeoffs, scope decisions).
- **{checkpoint-name}-spec.md**: The single in-progress spec. Named after the checkpoint it delivers. Contains all phases needed to reach that checkpoint. Moved to `archive/` when the checkpoint is complete.

### The Phased Planning Model

- A spec is broken into Phases.
- A single Phase must be scoped to fit in one session (a few hours at most): fully implemented, tested, and verified before moving on.
- Phases are defined in the spec file itself; no separate plan files.
- A spec may span multiple sessions but only one phase is active at a time.

### Phase Implementation Checklist

Specs for code checkpoints and infrastructure checkpoints follow different formats.

**Infrastructure checkpoints** (no code — server setup, environment configuration) have phases with setup steps and a verification checklist. The Contract / Tests / Implement steps do not apply.

**Code checkpoints** follow this loop. Before starting a phase, verify it has enough detail to proceed:

- Define Contract: Generate or update interfaces, types, or API signatures.
- Write Tests: Generate automated tests based on the contract and spec (before implementation).
- Implement: Generate the code to satisfy the tests.
- README Sync: Update the README to reflect the current state. The README must always be accurate and complete enough for a new developer to configure and run the application. Update: the Current State section to describe all working features; the Prerequisites section if new dependencies were added; the Configuration section if config files, keys, or secret paths changed; the Console Flow section if the interactive experience changed. Do not leave outdated instructions in place.
- Current Architecture Sync: Update current-architecture.md to reflect what was just built.
- Manual Verify: The user reviews and performs the Manual Verification Plan defined in the phase.
- Close Phase: Mark the phase status as Done in the spec.

When all phases in a spec are Done, move the spec file to `archive/`.

### Implementation Rules

- Contract-First: Never write implementation logic until the interface/types and tests exist.
- Red-Green-Sync: Implementation is only complete when tests pass and the documentation matches the final code.
- External API Verification: Verify exact API method signatures, property names, and available overloads against the installed package version before writing a phase. A phase may not begin implementation with unverified external API assumptions.

## Testing

- XUnit for C# Unit Tests
- Vitest for Vue Unit Tests
- Tests should not attempt to interact with a real ollama instance

## Git Flow

- The `main` branch is protected. All changes merge via pull request.
- Each spec gets one feature branch, named after the checkpoint (e.g., `spec/behavior-refactor`).
- The PR is opened when the spec is complete (all phases done) and ready for review.
- The user manages all git and GitHub commands manually. Do not run any git or GitHub commands unless explicitly instructed.
- Exception: agents running in an isolated worktree should commit their changes there. Use a descriptive commit message. Do not push.

## Security Policy

- **Never save in this workspace**: passwords, private keys, API keys, tokens, hostnames, usernames, or any value that identifies or grants access to a real system
- Use placeholder values in config examples (e.g., `your-host-here`, `your-api-key`)
- This applies to all files including gitignored ones — AI assistants read the full workspace
- **If you encounter anything in this workspace that looks potentially sensitive, call it out immediately before continuing**

## Other Notes

- Python is not available for scripting
- Node is avaialble for scripting
- Never use an icon or emoji in documentation or source code
- When making a manual verification plan, note any dependencies
- All code must be deployable on Linux: use `Path.Combine` for all paths, never hard-code backslashes, avoid Windows-only APIs
