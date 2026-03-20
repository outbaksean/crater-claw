# IDE Debugging Spec

## Name
- IDE Debugging

## Checkpoint
- ide-debugging

## Purpose
Configure VS Code to support C# debugging for `CraterClaw.Console` and `CraterClaw.Api`.

## Scope
- Add `.vscode/launch.json` with debug launch configurations for both projects.
- Add `build-console` and `build-api` pre-launch build tasks to `.vscode/tasks.json`.
- Update README to document how to launch the debugger.

---

## Phase 1: VS Code Launch Configuration

**Status: Done**

### Contract

`.vscode/launch.json` with two configurations:
- `Debug CraterClaw.Console` — launches the console harness via `coreclr`, uses `integratedTerminal` so interactive prompts work.
- `Debug CraterClaw.Api` — launches the API with `ASPNETCORE_ENVIRONMENT=Development` and `ASPNETCORE_URLS=http://localhost:5000`.

Both configurations specify a `preLaunchTask` that builds the relevant project before attaching.

`.vscode/tasks.json` gains two build tasks:
- `build-console` — `dotnet build .\CraterClaw.Console\CraterClaw.Console.csproj`
- `build-api` — `dotnet build .\CraterClaw.Api\CraterClaw.Api.csproj`

### Manual Verification Plan

1. Open the Run and Debug panel (`Ctrl+Shift+D`).
2. Select `Debug CraterClaw.Console`, press `F5`. Confirm the console harness starts in the integrated terminal and breakpoints are hit.
3. Select `Debug CraterClaw.Api`, press `F5`. Confirm the API starts and breakpoints are hit on incoming requests.
