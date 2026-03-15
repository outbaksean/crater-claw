# CraterClaw Library Behavior Profiles Plan

## Decisions

- `BehaviorProfile` is an immutable sealed record. `IBehaviorProfileService` exposes two synchronous methods — no async needed for an in-memory catalog.
- `GetById` returns `null` for an unknown identifier rather than throwing, keeping callers in control of the error message.
- `BehaviorProfileService` is registered as Singleton in `AddCraterClawCore()`. The catalog is stateless and allocated once.
- Profile identifiers are lowercase kebab-case strings matching the spec: `no-tools`, `qbittorrent-manager`.
- `AllowedMcpServerNames` uses the same keys as `McpServerDefinition.Name` in `craterclaw.json`.
- `RecommendedModelTags` is advisory metadata only. The catalog assigns `reasoning` to all profiles as a baseline; no enforcement is done by this layer.
- Console session state is a nullable local variable `selectedProfileId`. It is not persisted and has no effect on other console flows until the agentic execution spec is implemented.

## Overview
- Phase 1 defines contract types, the service interface, and automated tests.
- Phase 2 implements `BehaviorProfileService`, registers it in DI, and wires the console.

---

## Phase 1: Contracts and Tests

### Status
- Not Started

### Goal
- Define `BehaviorProfile`, `IBehaviorProfileService`, and write tests against the implementation that Phase 2 will produce.

### Contract
- `BehaviorProfile` sealed record in `CraterClaw.Core`:
  - `Id` (string) — stable lowercase kebab-case identifier
  - `Name` (string) — display name
  - `Description` (string)
  - `RecommendedModelTags` (IReadOnlyList\<string\>)
  - `AllowedMcpServerNames` (IReadOnlyList\<string\>)
- `IBehaviorProfileService` interface in `CraterClaw.Core`:
  - `IReadOnlyList<BehaviorProfile> GetAll()`
  - `BehaviorProfile? GetById(string id)`

### Tasks
- Add `BehaviorProfile.cs` and `IBehaviorProfileService.cs` in `CraterClaw.Core`.
- Add `BehaviorProfileServiceTests.cs` in `CraterClaw.Core.Tests`.

### Tests

`BehaviorProfileServiceTests`:
- `GetAll` returns exactly two profiles.
- All profile identifiers are unique (case-insensitive).
- `GetAll` contains profiles with identifiers: `no-tools`, `qbittorrent-manager`.
- `GetById` with a valid identifier returns the matching profile.
- `GetById` with an unknown identifier returns null.
- `no-tools` has an empty `AllowedMcpServerNames` list.
- `qbittorrent-manager` has `AllowedMcpServerNames` containing `qbittorrent`.

### Manual Verification
- `dotnet build CraterClaw.slnx` succeeds.
- `dotnet test CraterClaw.slnx --no-build` passes with the new tests included (skipped in red-green sense until Phase 2 provides the implementation).

---

## Phase 2: Implementation, DI Registration, and Console Wiring

### Status
- Not Started

### Goal
- Implement `BehaviorProfileService` with the hardcoded catalog, register it in DI, and add the profile selection flow to the console harness.

### Contract
- No new public surface beyond Phase 1.
- Console profile list format: `{n}. [{id}] {name} - {description}`
- After selection, console displays: `Permitted MCP servers: {name1}, {name2}` or `Permitted MCP servers: (none)` if the list is empty.

### Catalog

| Id | Name | Description | RecommendedModelTags | AllowedMcpServerNames |
|----|------|-------------|----------------------|-----------------------|
| `no-tools` | No Tools | General-purpose conversation and reasoning with no external tools. | `reasoning` | (none) |
| `qbittorrent-manager` | qBitTorrent Manager | Querying and managing downloads using qBitTorrent. | `reasoning` | `qbittorrent` |

### Tasks
- Add `BehaviorProfileService.cs` (internal, sealed) in `CraterClaw.Core`:
  - Hardcode the catalog as a private static readonly list initialized at class load.
  - `GetAll()` returns the list as-is.
  - `GetById(string id)` uses `StringComparison.OrdinalIgnoreCase`.
- Register `IBehaviorProfileService` as Singleton → `BehaviorProfileService` in `ServiceCollectionExtensions.AddCraterClawCore()`.
- Update `CraterClaw.Console/Program.cs`:
  - After the MCP section, retrieve all profiles via `IBehaviorProfileService.GetAll()`.
  - Display the numbered list with id, name, and description.
  - Prompt: `Select profile number (leave blank to skip):`.
  - On blank, skip without error.
  - On invalid input, print the standard out-of-range message and continue (do not exit).
  - On valid selection, display the permitted MCP server names and store the id in `selectedProfileId`.

### Tests
- No new tests required; Phase 1 tests cover all service behavior.

### Manual Verification Plan
- No external dependencies.
- Run the console harness and proceed past endpoint and model selection.
- Confirm all four profiles are listed with correct identifiers, names, and descriptions.
- Select `qbittorrent-manager` and confirm `Permitted MCP servers: qbittorrent` is displayed.
- Select `no-tools` and confirm `Permitted MCP servers: (none)` is displayed.
- Enter a blank selection and confirm the flow continues without error.
- Enter an out-of-range number and confirm an error message is shown and the flow continues.

---

## Completion Criteria
- Both phase statuses are marked Done.
- `CraterClaw.Core` and `CraterClaw.Console` build successfully.
- All automated tests pass.
- Manual verification confirms profile listing and selection work correctly.
- `behavior-profiles-spec.md` Status is updated to Done.
