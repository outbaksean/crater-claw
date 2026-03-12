# CraterClaw

## Spec-Based Development
### Structure & Hierarchy

- The Specs/ directory contains all Markdown specification and plan files.
- Hierarchy: Specs are organized by directory level (high-level to low-level).
- Mandatory Files: Each directory level must contain a {name}-spec.md.
- Convention: For a spec named {name}-spec.md, any children must live in a subdirectory named {name}/.
- Content Requirements: Every spec must include: Name, Purpose, Scope, and Status.
- Supporting Docs: Levels may include optional files like {name}-decisions.md (architectural choices) or {name}-prereqs.md (environment/dependency needs).
- Relationship: A child spec must represent a subset of the parent's scope but with significantly higher technical detail.

### Depth-First Development

- The spec tree is built top-down but implemented depth-first.
- A high-level spec may remain as a placeholder (Status: Planning, no plan, no child specs) indefinitely while implementation proceeds on a separate, fully-defined branch of the tree.
- Do not treat the absence of sibling plans or sibling child specs as a blocker. Only the active spec and its ancestors need to be sufficiently defined to begin implementation.
- When selecting what to work on next, choose the deepest spec that has a plan and is Not Started or In Progress.

### The Phased Planning Model

- Plan Scope: A spec that is ready for implementation has exactly one {name}-plan.md or a subdirectory of child specs — not both.
- Placeholder Specs: A spec with neither a plan nor child specs is a valid placeholder. It signals that scope has been acknowledged but not yet refined. Do not add a plan or child specs to a placeholder until it becomes the active implementation target.
- Atomic Phases: A plan.md is broken into Phases.
- The Session Rule: A single Phase must have a small enough scope to be fully implemented, tested, and verified in one AI session.
- Scalability: If a plan is too complex for a single file, it can refer to separate phase files (e.g., {name}-plan-p1.md).

Example Structure:
```text

Specs/
  ai-supervisor-spec.md
  ai-supervisor/
    library-spec.md
    library-plan.md         <-- Contains Phases 1, 2, and 3
    library/
      provider-spec.md      <-- Only if provider logic is too big for library-plan
      provider-plan.md
```

- Implementing a Phase (The Execution Loop)
- Before starting an AI session, verify the current Phase has enough detail to be "Contract-First" compliant. Use this checklist for every session:
#### Phase Implementation Checklist

- Define Contract: Generate or update interfaces, types, or API signatures.
- Write Tests: Generate automated tests based on the Contract and Spec (before implementation).
- Implement: Generate the code to satisfy the tests.
- Manual Verify: The user reviews and does the "Manual Verification Plan" defined in the plan.
- Sync Spec: If implementation forced a logic change, update the .md spec/plan and any relevant higher level specs.
- Close Session: Mark Phase status as Done.

#### Implementation Rules

- Contract-First: Never write implementation logic until the interface/types and tests exist.
- Integration Glue: A parent spec must define the I/O schemas relevant to the child spec currently being implemented. It does not need to define contracts for scope that has not yet been refined into an active child spec.
- Red-Green-Sync: Implementation is only complete when tests pass and the documentation matches the final code.
- Completion: A Spec is marked Done only when all associated Phases or Child Specs are Done.


## Testing
- XUnit for C# Unit Tests
- Vitest for Vue Unit Tests
- Tests should not attempt to interact with a real ollama instance

## Other Notes
- Python is not available for scripting
- Node is avaialble for scripting
- Never use an icon or emoji in documentation or source code
- Do not attempt any git or github commands unless explicitly instructed