# PowerShell Aliases Spec

## Name
- powershell-aliases

## Checkpoint
- powershell-aliases

## Purpose
Provide a `craterclaw` command available from any directory in PowerShell. Subcommands wrap common development tasks — running, building, testing, and formatting the solution — with sensible defaults and optional parameters.

## Design

### Module

A PowerShell module (`CraterClaw.psm1`) installed to the user's PowerShell modules directory (`$HOME\Documents\PowerShell\Modules\CraterClaw\`). Once installed and imported in the profile, `craterclaw` is available in any session from any directory.

The module locates the repository via an environment variable `CRATERCLAW_ROOT` set by the install script. This avoids hardcoding the path inside the module file.

### Install Script

`tools/Install-CraterClaw.ps1` — idempotent install script that:
1. Creates the module directory if it does not exist
2. Copies `CraterClaw.psm1` into it
3. Sets `CRATERCLAW_ROOT` as a persistent user-level environment variable pointing to the repository root
4. Adds `Import-Module CraterClaw` to the user's PowerShell profile if not already present
5. Prints confirmation of each step

### Command Interface

```
craterclaw <subcommand> [options]
```

#### `craterclaw run`
Starts the API and Vue dev server in separate terminal windows.
- `--api-only` — start only the API
- `--web-only` — start only the Vue dev server
- `--console` — start the console harness (interactive terminal, not a separate window)

#### `craterclaw build`
Builds the .NET solution.
- No options.

#### `craterclaw test`
Runs all tests: `dotnet test` on the solution and `npm test` in `CraterClaw.Web`.
- `--project <name>` — run tests for one project only. Accepted values: `core`, `api`, `web`.

#### `craterclaw format`
Runs `dotnet format` on the solution and `npm run lint:fix` in `CraterClaw.Web`.
- `--project <name>` — format one project only. Accepted values: `core`, `api`, `web`.
- `--check` — lint/format without fixing (exits non-zero if issues found); maps to `dotnet format --verify-no-changes` and `npm run lint`.

### Error Handling

Each subcommand should surface the exit code of the underlying process. If a process fails, print a clear message indicating which command failed and exit with a non-zero code.

---

## Phase 1: Module, Install Script, and All Commands

**Status: Done**

### Contract

Deliverables:
- `tools/CraterClaw.psm1` — PowerShell module
- `tools/Install-CraterClaw.ps1` — install script

No changes to C# or Vue source.

### Tests

No automated tests. Manual verification only.

### Implement

1. Create `tools/CraterClaw.psm1` implementing the `craterclaw` function with all subcommands and options described above.
2. Create `tools/Install-CraterClaw.ps1` with the idempotent install logic described above.

### README Sync

Add a `## Developer Commands` section to the README:
- One-time setup: how to run the install script
- The full command reference table
- Note that `CRATERCLAW_ROOT` must point to the repository root (set automatically by the install script)

### Current Architecture Sync

No changes needed.

### Manual Verification Plan

Prerequisites: install script has been run in a fresh session.

1. Open a new PowerShell session in a directory outside the repository.
2. Run `craterclaw build` — solution should build successfully.
3. Run `craterclaw test` — all tests should pass.
4. Run `craterclaw test --project core` — only core tests should run.
5. Run `craterclaw format --check` — should report format/lint status without modifying files.
6. Run `craterclaw run --api-only` — API should start in a separate window.
7. Run `craterclaw run --console` — console harness should start in the current terminal.
